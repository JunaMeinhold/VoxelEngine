//#define USE_LEGACY_LOADER

namespace VoxelEngine.Voxel
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Vortice.Direct3D11;
    using VoxelEngine.Core;

    public class WorldLoader : IDisposable
    {
        private readonly AutoResetEvent waitHandle = new(false);
        private readonly AutoResetEvent waitIOHandle = new(false);
        private Vector2[] indicesCache;

        // Contains the position history of the player.
        private readonly ConcurrentQueue<Vector3> positionQueue = new();

        // Contains chunks that are marked as dirty from a block update.
        private readonly ConcurrentQueue<ChunkRegion> updateQueue = new();

#if USE_LEGACY_LOADER

        // Contains chunks that needed to upload data to the gpu.
        private readonly ConcurrentQueue<ChunkRegion> uploadQueue = new();

#else

        // Contains chunks that needed to upload data to the gpu.
        private readonly ConcurrentQueue<RenderRegion> uploadQueue = new();

#endif

        // Contains chunks that will be added to LoadedChunks list.
        private readonly ConcurrentQueue<ChunkRegion> integrationQueue = new();

        // Contains chunks that will be unloaded form the simulation and LoadedChunks list.
        private readonly ConcurrentQueue<ChunkRegion> unloadQueue = new();

        // Contains chunks that will be unloaded from the gpu and will be send to unloadQueue.
        private readonly ConcurrentQueue<ChunkRegion> unloadIOQueue = new();

        // Contains chunks that are marked as loaded internal to prevent loading chunks multiple times.
        private readonly List<ChunkRegion> loadedInternal = new();

        // Constains chunks that will be rendered and simulated.
        private readonly List<Chunk> loadedChunks = new();

#if !USE_LEGACY_LOADER
        private readonly List<RenderRegion> renderRegions = new();
#endif
        private readonly Thread thread;
        private readonly Thread ioThread;
        private bool running = true;
        private bool idle = true;
        private bool ioIdle = true;

        public WorldLoader(World world)
        {
            World = world;
            thread = new Thread(LoadVoid);
            thread.Name = "ChunkLoaderThread";
            thread.Start();
            ioThread = new Thread(IOVoid);
            ioThread.Name = "ChunkLoaderIOThread";
            ioThread.Start();
            RegenerateCache();
        }

        public World World { get; }

        public IReadOnlyList<Chunk> LoadedChunks => loadedChunks;

        public IReadOnlyList<ChunkRegion> LoadedChunkRegions => loadedInternal;
#if !USE_LEGACY_LOADER
        public IReadOnlyList<RenderRegion> LoadedRenderRegions => renderRegions;
#endif
        public bool Idle => idle;

        public bool IOIdle => ioIdle;

        public int UpdateQueueCount => updateQueue.Count;

        public int UploadQueueCount => uploadQueue.Count;

        public int UnloadQueueCount => unloadQueue.Count;

        public int UnloadIOQueueCount => unloadIOQueue.Count;
#if !USE_LEGACY_LOADER
        public int RenderRegionCount => renderRegions.Count;
#else
        public int RenderRegionCount => 0;
#endif
        public int ChunkRegionCount => loadedInternal.Count;

        public int ChunkCount => loadedChunks.Count;

        private static IEnumerable<Vector2> GetIndices(Vector3 center)
        {
            int halfDistance = (int)(Nucleus.Settings.ChunkRenderDistance * 0.5f);
            for (int x = -halfDistance; x < halfDistance; x++)
            {
                for (int z = -halfDistance; z < halfDistance; z++)
                {
                    yield return new Vector2(x, z) + new Vector2(center.X, center.Z);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegenerateCache()
        {
            indicesCache = GetIndices(Vector3.Zero).ToArray();

            static int compare(Vector2 a, Vector2 b)
            {
                float da = Vector2.Distance(Vector2.Zero, a);
                float db = Vector2.Distance(Vector2.Zero, b);

                if (da < db)
                {
                    return -1;
                }
                else if (db < da)
                {
                    return 1;
                }

                return 0;
            }

            Array.Sort(indicesCache, compare);
        }

#if !USE_LEGACY_LOADER

        private RenderRegion FindRenderRegion(Vector2 pos)
        {
            for (int i = 0; i < renderRegions.Count; i++)
            {
                RenderRegion renderRegion = renderRegions[i];
                if (renderRegion.ContainsRegionPos(pos))
                {
                    return renderRegion;
                }
            }
            const int regionSize = 4;
            float x = pos.X % regionSize;
            float y = pos.Y % regionSize;

            Vector2 p = pos - new Vector2(x, y);
            if (x < 0)
            {
                p.X -= regionSize;
            }
            if (y < 0)
            {
                p.Y -= regionSize;
            }
            RenderRegion region = new(p, new(regionSize));
            renderRegions.Add(region);

            return region;
        }

#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(Vector3 vector)
        {
            positionQueue.Clear();
            positionQueue.Enqueue(vector);
            waitHandle.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(Chunk chunk)
        {
            chunk.UnloadFormSimulation();
            ChunkRegion region = World.GetRegion(chunk.Position);
            updateQueue.Enqueue(region);
            waitHandle.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Upload(ID3D11Device device, ID3D11DeviceContext context)
        {
            if (uploadQueue.IsEmpty & unloadQueue.IsEmpty)
            {
                return;
            }
#if USE_LEGACY_LOADER
            while (uploadQueue.TryDequeue(out ChunkRegion region))
            {
                region.Upload(device);
            }
#else
            while (uploadQueue.TryDequeue(out RenderRegion region))
            {
                region.Update(device, context);
            }
#endif
            while (integrationQueue.TryDequeue(out ChunkRegion region))
            {
                loadedChunks.AddRange(region.Chunks);
            }

            while (unloadQueue.TryDequeue(out ChunkRegion region))
            {
                region.Unload();
                for (int i = 0; i < region.Chunks.Length; i++)
                {
                    Chunk chunk = region.Chunks[i];
                    chunk.UnloadFormSimulation();
                    _ = loadedChunks.Remove(chunk);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreLoadBatch(Vector3 chunkVGlobal)
        {
            if (!World.InChunkBounds((int)chunkVGlobal.X, 0, (int)chunkVGlobal.Z))
            {
                return;
            }

            ChunkRegion region = World.GetRegion(chunkVGlobal);
            if (loadedInternal.Contains(region))
            {
                return;
            }

            if (region.IsEmpty | !region.InMemory)
            {
                if (region.ExistOnDisk(World))
                {
                    region.LoadFromDiskUnsafe(World);
                }
                else
                {
                    region.Generate(World);
                }
            }

            if (!region.InMemory)
            {
                return;
            }

            region.Load();
            loadedInternal.Add(region);
            integrationQueue.Enqueue(region);
#if USE_LEGACY_LOADER
            uploadQueue.Enqueue(region);
#else
            RenderRegion renderRegion = FindRenderRegion(region.Position);
            renderRegion.ChunkRegions.Add(region);
            if (!uploadQueue.Contains(renderRegion))
            {
                uploadQueue.Enqueue(renderRegion);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unload(ChunkRegion region)
        {
            if (region.IsEmpty)
            {
                return;
            }
#if !USE_LEGACY_LOADER
            RenderRegion renderRegion = FindRenderRegion(region.Position);
            renderRegion.ChunkRegions.Remove(region);
            if (renderRegion.ChunkRegions.Count == 0)
            {
                renderRegions.Remove(renderRegion);
                renderRegion.Release();
            }
            else
            {
                uploadQueue.Enqueue(renderRegion);
            }
#endif

            region.Save();
            _ = loadedInternal.Remove(region);
        }

        private readonly HashSet<Vector2> loadedChunksIndices = new(Nucleus.Settings.ChunkRenderDistance * Nucleus.Settings.ChunkRenderDistance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadVoid()
        {
            while (running)
            {
                while (updateQueue.TryDequeue(out ChunkRegion region) && running)
                {
                    region.Load();
#if USE_LEGACY_LOADER
                    uploadQueue.Enqueue(region);
#else
                    RenderRegion renderRegion = FindRenderRegion(region.Position);
                    if (!uploadQueue.Contains(renderRegion))
                    {
                        uploadQueue.Enqueue(renderRegion);
                    }
#endif
                }

                if (positionQueue.TryDequeue(out Vector3 pos) && running)
                {
                    loadedChunksIndices.Clear();

                    // Load chunks
                    for (int i = 0; i < indicesCache.Length; i++)
                    {
                        Vector2 vector = indicesCache[i];
                        Vector2 vec = new(vector.X + pos.X, vector.Y + pos.Z);
                        loadedChunksIndices.Add(vec);
                        PreLoadBatch(new Vector3(vec.X, 0, vec.Y));
                    }

                    // Unload chunks
                    for (int i = 0; i < loadedInternal.Count; i++)
                    {
                        ChunkRegion region = loadedInternal[i];
                        if (!loadedChunksIndices.Contains(region.Position))
                        {
                            unloadIOQueue.Enqueue(region);
                        }
                    }

                    waitIOHandle.Set();
                }

                if (running)
                {
                    idle = true;
                    waitHandle.WaitOne();
                    idle = false;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IOVoid()
        {
            while (running)
            {
                while (unloadIOQueue.TryDequeue(out ChunkRegion region))
                {
                    Unload(region);
                    unloadQueue.Enqueue(region);
                }

                if (running)
                {
                    ioIdle = true;
                    waitIOHandle.WaitOne();
                    ioIdle = false;
                }
            }
        }

        public void Dispose()
        {
            running = false;
            waitHandle.Set();
            waitIOHandle.Set();
            thread.Join();
            ioThread.Join();

            for (int i = 0; i < loadedInternal.Count; i++)
            {
                ChunkRegion region = loadedInternal[i];
                region.Unload();
            }

#if !USE_LEGACY_LOADER
            for (int i = 0; i < renderRegions.Count; i++)
            {
                renderRegions[i].Release();
            }
#endif

            loadedInternal.Clear();
            GC.SuppressFinalize(this);
        }
    }
}