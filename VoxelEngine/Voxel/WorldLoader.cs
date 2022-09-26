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
        private AutoResetEvent waitHandle = new(false);
        private Vector2[] indicesCache;

        // Contains the position history of the player.
        private readonly ConcurrentQueue<Vector3> positionQueue = new();

        // Contains chunks that are marked as dirty from a block update.
        private readonly ConcurrentQueue<ChunkRegion> updateQueue = new();

        // Contains chunks that needed to upload data to the gpu.
        private readonly ConcurrentQueue<ChunkRegion> uploadQueue = new();

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

        private readonly Thread thread;
        private bool running = true;

        public WorldLoader(World world)
        {
            World = world;
            thread = new Thread(LoadVoid);
            thread.Name = "ChunkLoaderThread";
            thread.Start();
            RegenerateCache();
        }

        public World World { get; }

        public IReadOnlyList<Chunk> LoadedChunks => loadedChunks;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(Vector3 vector)
        {
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
        public void Upload(ID3D11Device device)
        {
            if (uploadQueue.IsEmpty & unloadQueue.IsEmpty)
            {
                return;
            }

            while (uploadQueue.TryDequeue(out ChunkRegion region))
            {
                region.Upload(device);
            }

            while (integrationQueue.TryDequeue(out ChunkRegion region))
            {
                loadedChunks.AddRange(region.Chunks);
            }

            while (unloadQueue.TryDequeue(out ChunkRegion region))
            {
                region.Unload();
                foreach (Chunk chunk in region.Chunks)
                {
                    chunk.UnloadFormSimulation();
                    _ = loadedChunks.Remove(chunk);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreLoadBatch(Vector3 chunkV)
        {
            if (!World.InChunkBounds((int)chunkV.X, 0, (int)chunkV.Z))
            {
                return;
            }

            ChunkRegion region = World.GetRegion(chunkV);
            if (loadedInternal.Contains(region))
            {
                return;
            }

            if (region.IsEmpty | !region.InMemory)
            {
                if (region.ExistOnDisk(World))
                {
                    region.LoadFromDisk(World);
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
            uploadQueue.Enqueue(region);
            integrationQueue.Enqueue(region);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unload(ChunkRegion region)
        {
            if (region.IsEmpty)
            {
                return;
            }

            region.Save();
            _ = loadedInternal.Remove(region);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadVoid()
        {
            while (running)
            {
                while (updateQueue.TryDequeue(out ChunkRegion region) && running)
                {
                    region.Load();
                    uploadQueue.Enqueue(region);
                }

                if (positionQueue.TryDequeue(out Vector3 pos) && running)
                {
                    HashSet<Vector2> loadedChunks = new(Nucleus.Settings.ChunkRenderDistance * Nucleus.Settings.ChunkRenderDistance);

                    foreach (Vector2 vector in indicesCache)
                    {
                        Vector2 vec = new(vector.X + pos.X, vector.Y + pos.Z);
                        loadedChunks.Add(vec);
                        PreLoadBatch(new Vector3(vec.X, 0, vec.Y));
                    }

                    // Unload Chunks
                    foreach (ChunkRegion region in loadedInternal)
                    {
                        if (!loadedChunks.Contains(region.Position))
                        {
                            unloadIOQueue.Enqueue(region);
                        }
                    }

                    while (unloadIOQueue.TryDequeue(out ChunkRegion region))
                    {
                        Unload(region);
                        unloadQueue.Enqueue(region);
                    }
                }

                if (running)
                {
                    waitHandle.Set();
                }
            }
        }

        public void Dispose()
        {
            running = false;
            waitHandle.Set();
            thread.Join();

            foreach (ChunkRegion region in loadedInternal)
            {
                region.Unload();
            }

            loadedInternal.Clear();
            GC.SuppressFinalize(this);
        }
    }
}