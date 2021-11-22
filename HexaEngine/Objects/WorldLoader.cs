namespace HexaEngine.Objects
{
    using HexaEngine.Windows;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Threading;
    using VoxelGen;

    public class WorldLoader : Disposable
    {
        private readonly ConcurrentQueue<Vector3> positionQueue = new();
        private readonly ConcurrentQueue<ChunkRegion> updateQueue = new();
        private readonly ConcurrentQueue<ChunkRegion> uploadQueue = new();
        private readonly ConcurrentQueue<ChunkRegion> unloadQueue = new();
        private readonly ConcurrentQueue<ChunkRegion> unloadIOQueue = new();
        private readonly List<ChunkRegion> loadedInternal = new();
        private readonly List<ChunkRegion> cpuInternal = new();
        private readonly List<ChunkRegion> gpuInternal = new();
        private readonly Thread thread;
        private bool running = true;
        private Vector3 currentPos;

        public WorldLoader(World world)
        {
            World = world;
            thread = new Thread(LoadVoid);
            thread.Start();
        }

        private World World { get; }

        public List<Chunk> LoadedChunks { get; } = new();

        public int RenderDistance { get; set; } = 32;

        public int DiskRadius { get; set; } = 1;

        public int RenderRadius => RenderDistance / 2;

        public void Dispatch(Vector3 vector)
        {
            positionQueue.Enqueue(vector);
        }

        public void Dispatch(Chunk chunk)
        {
            updateQueue.Enqueue(new ChunkRegion() { Position = new(chunk.chunkPosX, chunk.chunkPosZ), Chunks = new Chunk[] { chunk } });
        }

        public void Refresh()
        {
            foreach (var region in loadedInternal)
            {
                region.Load();
                uploadQueue.Enqueue(region);
            }
        }

        public void Upload()
        {
            if (uploadQueue.IsEmpty & unloadQueue.IsEmpty) return;
            while (uploadQueue.TryDequeue(out ChunkRegion region))
            {
                region.Upload();
                LoadedChunks.AddRange(region.Chunks);
            }
            while (unloadQueue.TryDequeue(out ChunkRegion region))
            {
                foreach (Chunk chunk in region.Chunks)
                    _ = LoadedChunks.Remove(chunk);
            }
        }

        private void LoadBatch(Vector3 chunkV)
        {
            if (chunkV.X >= 0 & chunkV.X < WorldMap.MAP_SIZE_X / 2)
            {
                if (chunkV.Z >= 0 & chunkV.Z < WorldMap.MAP_SIZE_Z / 2)
                {
                    var region = World.GetRegion(chunkV);
                    if (loadedInternal.Contains(region)) return;
                    if (region.IsEmpty | (!region.InMemory))
                    {
                        if (region.ExistOnDisk(World))
                            region.LoadFromDisk(World);
                        else
                            region.Generate(World);
                    }
                    if (!region.InMemory) return;
                    region.Load();
                    loadedInternal.Add(region);
                    uploadQueue.Enqueue(region);
                }
            }
        }

        private void LoadBatchCpu(Vector3 chunkV)
        {
            if (chunkV.X >= 0 & chunkV.X < WorldMap.MAP_SIZE_X / 2)
            {
                if (chunkV.Z >= 0 & chunkV.Z < WorldMap.MAP_SIZE_Z / 2)
                {
                    var region = World.GetRegion(chunkV);
                    if (loadedInternal.Contains(region)) return;
                    if (region.IsEmpty | (!region.InMemory))
                    {
                        if (region.ExistOnDisk(World))
                            region.LoadFromDisk(World);
                        else
                            region.Generate(World);
                    }
                    if (!region.InMemory) return;
                    region.Load();
                    loadedInternal.Add(region);
                    uploadQueue.Enqueue(region);
                }
            }
        }

        private void LoadBatchGpu(Vector3 chunkV)
        {
            if (chunkV.X < 0 | chunkV.X >= WorldMap.MAP_SIZE_X / 2)
            {
                return;
            }
            if (chunkV.Z < 0 | chunkV.Z >= WorldMap.MAP_SIZE_Z / 2)
            {
                return;
            }

            var region = World.GetRegion(chunkV);

            if (loadedInternal.Contains(region))
            {
                return;
            }

            if (!region.ExistOnDisk(World))
                region.Generate(World);
            else
                region.LoadFromDisk(World);
            if (region.GetState(World) == ChunkState.None) return;
            region.UpdateState(ChunkState.OnGpu);
            loadedInternal.Add(region);
            uploadQueue.Enqueue(region);
        }

        private void Unload(ChunkRegion region)
        {
            if (region.IsEmpty)
                return;

            region.DeepUnload();
            _ = loadedInternal.Remove(region);
        }

        private void LoadVoid()
        {
            while (running)
            {
                while (updateQueue.TryDequeue(out var region) && running)
                {
                    region.Load();
                    uploadQueue.Enqueue(region);
                }

                while (positionQueue.TryDequeue(out var pos) && running)
                {
                    currentPos = pos;
                    var loadedChunks = new Vector2[RenderDistance * RenderDistance];
                    int i = 0;

                    for (int x = -RenderRadius; x < RenderRadius; x++)
                    {
                        for (int z = -RenderRadius; z < RenderRadius; z++)
                        {
                            var chunkV = new Vector3(x, 0, z) + pos;
                            loadedChunks[i] = new(chunkV.X, chunkV.Z);
                            LoadBatchGpu(chunkV);
                            i++;
                        }
                    }

                    // Unload Chunks
                    foreach (var region in loadedInternal)
                    {
                        if (!loadedChunks.Contains(region.Position))
                        {
                            unloadIOQueue.Enqueue(region);
                        }
                    }

                    while (unloadIOQueue.TryDequeue(out var region))
                    {
                        Unload(region);
                    }
                }
                while (positionQueue.IsEmpty && updateQueue.IsEmpty && running)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void Wait()
        {
            while (thread.IsAlive)
            {
                Thread.Sleep(1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            running = false;
            Wait();
            foreach (ChunkRegion region in loadedInternal)
            {
                region.DeepUnload();
            }
            loadedInternal.Clear();
            base.Dispose(disposing);
        }
    }
}