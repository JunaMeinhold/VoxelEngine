namespace VoxelEngine.Voxel
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using BepuUtilities.Memory;
    using Vortice.Direct3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Threading;

    public class WorldLoader : IDisposable
    {
        private Vector2[] indicesRenderCache;
        private Vector2[] indicesPreloadCache;

        private readonly HashSet<Vector2> loadedRegionIds = new();

        private readonly ConcurrentQueue<ChunkSegment> loadIOQueue = new();

        private readonly ConcurrentQueue<ChunkSegment> generationQueue = new();

        private readonly ConcurrentQueue<ChunkSegment> loadQueue = new();

        // Contains chunks that are marked as dirty from an block update.
        private readonly ConcurrentQueue<ChunkSegment> updateQueue = new();

        // Contains chunks that needed to upload data to the gpu.
        private readonly ConcurrentQueue<RenderRegion> uploadQueue = new();

        // Contains chunks that will be added to LoadedChunks list.
        private readonly ConcurrentQueue<ChunkSegment> integrationQueue = new();

        // Contains chunks that will be unloaded form the simulation and LoadedChunks list.
        private readonly ConcurrentQueue<ChunkSegment> unloadQueue = new();

        // Contains chunks that will be unloaded from the gpu and will be send to unloadQueue.
        private readonly ConcurrentQueue<ChunkSegment> unloadIOQueue = new();

        private readonly ConcurrentQueue<ChunkSegment> saveIOQueue = new();

        // Contains chunks that are marked as loaded internal to prevent loading chunks multiple times.
        private readonly ConcurrentList<ChunkSegment> loadedInternal = new();

        // Contains chunks that will be rendered and simulated.
        private readonly List<Chunk> loadedChunks = new();

        private readonly ConcurrentList<RenderRegion> renderRegions = new();

        private readonly Worker[] workers;
        private readonly Worker[] ioWorkers;
        private bool running = true;
        private int idle = 0;
        private int ioIdle = 0;

        private readonly SemaphoreSlim semaphore = new(1);

        public struct Worker
        {
            public int Id;
            public Thread Thread;
            public AutoResetEvent Handle;
            public BufferPool Pool;
        }

        public WorldLoader(World world, int threads = 8, int ioThreads = 1)
        {
            World = world;
            workers = new Worker[threads];
            idle = threads;
            ioIdle = ioThreads;
            for (int i = 0; i < threads; i++)
            {
                workers[i].Id = i;
                workers[i].Handle = new AutoResetEvent(false);
                workers[i].Thread = new Thread(LoadVoid);
                workers[i].Thread.Name = "ChunkLoader Worker";
                workers[i].Thread.Start(i);
                workers[i].Pool = new();
            }

            ioWorkers = new Worker[ioThreads];

            for (int i = 0; i < ioThreads; i++)
            {
                ioWorkers[i].Id = i;
                ioWorkers[i].Handle = new AutoResetEvent(false);
                ioWorkers[i].Thread = new Thread(IOVoid);
                ioWorkers[i].Thread.Name = "ChunkLoader IO Worker";
                ioWorkers[i].Thread.Start(i);
            }

            RegenerateCache();
        }

        public World World { get; }

        public IReadOnlyList<Chunk> LoadedChunks => loadedChunks;

        public IReadOnlyList<ChunkSegment> LoadedChunkSegments => loadedInternal;

        public IReadOnlyList<RenderRegion> LoadedRenderRegions => renderRegions;

        public bool Idle => idle == 0;

        public bool IOIdle => ioIdle == 0;

        public int UpdateQueueCount => updateQueue.Count;

        public int UploadQueueCount => uploadQueue.Count;

        public int GenerationQueueCount => generationQueue.Count;

        public int LoadQueueCount => loadQueue.Count;

        public int UnloadQueueCount => unloadQueue.Count;

        public int UnloadIOQueueCount => unloadIOQueue.Count;

        public int LoadIOQueueCount => loadIOQueue.Count;

        public int SaveIOQueueCount => saveIOQueue.Count;

        public int RenderRegionCount => renderRegions.Count;

        public int ChunkSegmentCount => loadedInternal.Count;

        public int ChunkCount => loadedChunks.Count;

        public bool DoNotSave { get; set; } = true;

        private static IEnumerable<Vector2> GetIndices(Vector3 center, int radius)
        {
            for (int x = -radius; x < radius; x++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    yield return new Vector2(x, z) + new Vector2(center.X, center.Z);
                }
            }
        }

        private void SignalWorkers()
        {
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Handle.Set();
            }
        }

        private void SignalIOWorkers()
        {
            for (int i = 0; i < ioWorkers.Length; i++)
            {
                ioWorkers[i].Handle.Set();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegenerateCache()
        {
            indicesRenderCache = GetIndices(Vector3.Zero, Nucleus.Settings.ChunkRenderDistance).ToArray();
            indicesPreloadCache = GetIndices(Vector3.Zero, Nucleus.Settings.ChunkRenderDistance).ToArray();
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

            Array.Sort(indicesRenderCache, compare);
            Array.Sort(indicesPreloadCache, compare);
        }

        private RenderRegion FindRenderRegion(Vector2 pos)
        {
            semaphore.Wait();
            for (int i = 0; i < renderRegions.Count; i++)
            {
                RenderRegion renderRegion = renderRegions[i];
                if (renderRegion.ContainsRegionPos(pos))
                {
                    semaphore.Release();
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

            RenderRegion region;

            region = new(p, new(regionSize));
            renderRegions.Add(region);

            semaphore.Release();
            return region;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(Vector3 pos)
        {
            loadedChunksIndices.Clear();

            // Load chunks
            for (int i = 0; i < indicesRenderCache.Length; i++)
            {
                Vector2 vector = indicesRenderCache[i];
                Vector2 vec = new(vector.X + pos.X, vector.Y + pos.Z);
                loadedChunksIndices.Add(vec);
                Vector3 chunkVGlobal = new(vec.X, 0, vec.Y);

                if (loadedRegionIds.Contains(vec))
                {
                    continue;
                }

                loadedRegionIds.Add(vec);

                if (!World.InWorldLimits((int)chunkVGlobal.X, 0, (int)chunkVGlobal.Z))
                {
                    continue;
                }

                ChunkSegment segment = World.GetSegment(chunkVGlobal);
                loadIOQueue.Enqueue(segment);
            }

            // Unload chunks
            for (int i = 0; i < loadedInternal.Count; i++)
            {
                ChunkSegment segment = loadedInternal[i];
                if (!loadedChunksIndices.Contains(segment.Position))
                {
                    unloadIOQueue.Enqueue(segment);
                }
                loadedRegionIds.Remove(segment.Position);
            }

            SignalIOWorkers();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(Chunk chunk)
        {
            chunk.UnloadFormSimulation();
            ChunkSegment segment = World.GetSegment(chunk.Position);
            if (updateQueue.Contains(segment))
            {
                return;
            }
            updateQueue.Enqueue(segment);
            saveIOQueue.Enqueue(segment);
            SignalIOWorkers();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Upload(ID3D11Device device, ID3D11DeviceContext context)
        {
            if (uploadQueue.IsEmpty & unloadQueue.IsEmpty)
            {
                return;
            }

            while (uploadQueue.TryDequeue(out RenderRegion region))
            {
                region.Update(device, context);
            }

            while (integrationQueue.TryDequeue(out ChunkSegment segment))
            {
                loadedChunks.AddRange(segment.Chunks);
            }

            while (unloadQueue.TryDequeue(out ChunkSegment segment))
            {
                for (int i = 0; i < segment.Chunks.Length; i++)
                {
                    Chunk chunk = segment.Chunks[i];
                    chunk.UnloadFromGPU();
                    chunk.UnloadFormSimulation();
                    _ = loadedChunks.Remove(chunk);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadIORegion(ChunkSegment segment)
        {
            if (loadedInternal.Contains(segment))
            {
                return;
            }

            if (segment.IsEmpty | !segment.InMemory)
            {
                if (segment.ExistOnDisk(World))
                {
                    segment.LoadFromDisk(World);
                }
                else
                {
                    generationQueue.Enqueue(segment);
                }
            }

            loadQueue.Enqueue(segment);
        }

        private void GenerateRegion(ChunkSegment segment)
        {
            segment.Generate(World);
            saveIOQueue.Enqueue(segment);
            loadQueue.Enqueue(segment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadRegion(ChunkSegment segment, BufferPool pool)
        {
            if (!segment.InMemory)
            {
                return;
            }

            segment.Load(pool);

            loadedInternal.Add(segment);
            integrationQueue.Enqueue(segment);

            RenderRegion renderRegion = FindRenderRegion(segment.Position);
            renderRegion.AddRegion(segment);
            if (!uploadQueue.Contains(renderRegion))
            {
                uploadQueue.Enqueue(renderRegion);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateRegion(ChunkSegment segment, BufferPool pool)
        {
            if (!segment.InMemory)
            {
                return;
            }

            segment.Load(pool);

            RenderRegion renderRegion = FindRenderRegion(segment.Position);
            if (!uploadQueue.Contains(renderRegion))
            {
                uploadQueue.Enqueue(renderRegion);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnloadIORegion(ChunkSegment segment)
        {
            if (segment.IsEmpty)
            {
                return;
            }

            RenderRegion renderRegion = FindRenderRegion(segment.Position);
            renderRegion.RemoveRegion(segment);
            if (renderRegion.RegionCount == 0)
            {
                renderRegions.Remove(renderRegion);
                renderRegion.Release();
            }
            else
            {
                uploadQueue.Enqueue(renderRegion);
            }

            segment.SaveToDisk();
            segment.UnloadFromMem();
            loadedInternal.Remove(segment);
            unloadQueue.Enqueue(segment);
        }

        private readonly HashSet<Vector2> loadedChunksIndices = new(Nucleus.Settings.ChunkRenderDistance * 2 * Nucleus.Settings.ChunkRenderDistance * 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadVoid(object param)
        {
            int id = (int)param;
            Worker worker = workers[id];
            while (running)
            {
                {
                    if (updateQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        UpdateRegion(segment, worker.Pool);
                    }
                }

                bool gen = !generationQueue.IsEmpty;
                {
                    if (generationQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        GenerateRegion(segment);
                    }
                }
                if (gen)
                {
                    SignalIOWorkers();
                }

                {
                    if (loadQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        LoadRegion(segment, worker.Pool);
                    }
                }

                if (running && updateQueue.IsEmpty && generationQueue.IsEmpty && loadQueue.IsEmpty)
                {
                    Interlocked.Decrement(ref idle);
                    worker.Handle.WaitOne();
                    Interlocked.Increment(ref idle);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IOVoid(object param)
        {
            int id = (int)param;
            Worker worker = ioWorkers[id];
            while (running)
            {
                while (unloadIOQueue.TryDequeue(out ChunkSegment segment) && running)
                {
                    UnloadIORegion(segment);
                }

                while (saveIOQueue.TryDequeue(out ChunkSegment segment) && running)
                {
                    if (!DoNotSave)
                    {
                        segment.SaveToDisk();
                    }
                }

                while (loadIOQueue.TryDequeue(out ChunkSegment segment) && running)
                {
                    LoadIORegion(segment);
                }

                SignalWorkers();

                if (running)
                {
                    Interlocked.Decrement(ref ioIdle);
                    worker.Handle.WaitOne();
                    Interlocked.Increment(ref ioIdle);
                }
            }
        }

        public void Dispose()
        {
            running = false;

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Handle.Set();
                workers[i].Thread.Join();
            }

            for (int i = 0; i < ioWorkers.Length; i++)
            {
                ioWorkers[i].Handle.Set();
                ioWorkers[i].Thread.Join();
            }

            for (int i = 0; i < loadedInternal.Count; i++)
            {
                ChunkSegment segment = loadedInternal[i];
                segment.Unload();
            }

            for (int i = 0; i < renderRegions.Count; i++)
            {
                renderRegions[i].Release();
            }

            loadedInternal.Clear();
            GC.SuppressFinalize(this);
        }
    }
}