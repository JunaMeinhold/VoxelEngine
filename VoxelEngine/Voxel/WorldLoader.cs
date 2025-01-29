namespace VoxelEngine.Voxel
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using BepuUtilities.Memory;
    using Hexa.NET.Utilities;
    using Hexa.NET.D3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Threading;
    using HexaGen.Runtime.COM;

    public readonly struct SpatialSorter : IComparer<Vector2>
    {
        public static readonly SpatialSorter Default;

        public int Compare(Vector2 x, Vector2 y)
        {
            float da = Vector2.Distance(Vector2.Zero, x);
            float db = Vector2.Distance(Vector2.Zero, y);

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
    }

    public class LockedQueue<T>
    {
        private readonly Queue<T> queue = [];
        private readonly Lock _lock = new();
        private volatile int count;

        public int Count => count;

        public bool IsEmpty => count == 0;

        public void Lock()
        {
            _lock.Enter();
        }

        public void ReleaseLock()
        {
            _lock.Exit();
        }

        public bool Contains(T item)
        {
            Lock();
            var result = queue.Contains(item);
            ReleaseLock();
            return result;
        }

        public void EnqueueUnsafe(T item)
        {
            queue.Enqueue(item);
            count++;
        }

        public void EnqueueRange(IList<T> values)
        {
            Lock();
            int count = values.Count;
            queue.EnsureCapacity(queue.Count + count);
            foreach (T item in values)
            {
                queue.Enqueue(item);
            }
            this.count += count;
            ReleaseLock();
        }

        public void Enqueue(T item)
        {
            Lock();
            queue.Enqueue(item);
            count++;
            ReleaseLock();
        }

        public T Dequeue()
        {
            Lock();
            T item = queue.Dequeue();
            count--;
            ReleaseLock();
            return item;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            Lock();
            bool item = queue.TryDequeue(out result);
            if (item)
            {
                count--;
            }

            ReleaseLock();
            return item;
        }
    }

    public class WorldLoader : IDisposable
    {
        private Vector2[] indicesRenderCache;
        private HashSet<Vector2> indicesSimulationCache;

        private readonly HashSet<Vector2> loadedRegionIds = new();

        private readonly LockedQueue<Vector3> loadIOQueue = new();

        private readonly LockedQueue<ChunkSegment> generationQueue = new();

        private readonly LockedQueue<ChunkSegment> loadQueue = new();

        // Contains chunks that are marked as dirty from an block update.
        private readonly LockedQueue<ChunkSegment> updateQueue = new();

        // Contains chunks that needed to upload data to the gpu.
        private readonly LockedQueue<RenderRegion> uploadQueue = new();

        // Contains chunks that will be added to LoadedChunks list.
        private readonly LockedQueue<ChunkSegment> integrationQueue = new();

        // Contains chunks that will be unloaded form the simulation and LoadedChunks list.
        private readonly LockedQueue<ChunkSegment> unloadQueue = new();

        // Contains chunks that will be unloaded from the gpu and will be send to unloadQueue.
        private readonly LockedQueue<ChunkSegment> unloadIOQueue = new();

        private readonly LockedQueue<ChunkSegment> saveIOQueue = new();

        // Contains chunks that are marked as loaded internal to prevent loading chunks multiple times.
        private readonly ConcurrentList<ChunkSegment> loadedInternal = new();

        // Contains chunks that will be rendered and simulated.
        private readonly List<Chunk> loadedChunks = new();

        private readonly ConcurrentList<RenderRegion> renderRegions = new();

        private readonly Worker[] workers;
        private readonly Worker[] ioWorkers;
        private bool running = true;
        private int idle;
        private int ioIdle;

        private readonly SemaphoreSlim semaphore = new(1);

        public class Worker
        {
            public int Id;
            public Thread Thread;
            public AutoResetEvent Handle;
            public BufferPool Pool;
        }

        public WorldLoader(World world, int threads = 4, int ioThreads = 2)
        {
            World = world;
            workers = new Worker[threads];
            idle = threads;
            ioIdle = ioThreads;
            for (int i = 0; i < threads; i++)
            {
                workers[i] = new Worker()
                {
                    Id = i,
                    Handle = new AutoResetEvent(false),
                    Thread = new Thread(LoadVoid)
                    {
                        Name = $"ChunkLoader Worker {i}"
                    },
                    Pool = new()
                };

                workers[i].Thread.Start(i);
            }

            ioWorkers = new Worker[ioThreads];

            for (int i = 0; i < ioThreads; i++)
            {
                ioWorkers[i] = new Worker()
                {
                    Id = i,
                    Handle = new AutoResetEvent(false),
                    Thread = new Thread(IOVoid)
                    {
                        Name = $"ChunkLoader IO Worker {i}"
                    },
                };

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

        public int RenderDistance { get; private set; } = 16;

        private static IEnumerable<Vector2> GetIndices(Vector3 center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
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
            indicesSimulationCache = [.. GetIndices(Vector3.Zero, Nucleus.Settings.ChunkSimulationDistance).Order(SpatialSorter.Default)];
            Array.Sort(indicesRenderCache, compare);
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

        private Vector3 lastPos;

        public unsafe void DispatchInitial(Vector3 pos)
        {
            lastPos = pos;

            MiniProfiler.Instance.Clear();
            MiniProfiler.Instance.Begin("Dispatch.Total");
            MiniProfiler.Instance.Begin("Dispatch.Load");
            loadedChunksIndices.Clear();

            for (int i = 0; i < indicesRenderCache.Length; i++)
            {
                Vector2 vector = indicesRenderCache[i];
                Vector2 vec = new(vector.X + pos.X, vector.Y + pos.Z);
                loadedChunksIndices.Add(vec);
                Vector3 chunkVGlobal = new(vec.X, 0, vec.Y);

                if (!loadedRegionIds.Add(vec))
                {
                    continue;
                }

                if (!World.InWorldLimits((int)chunkVGlobal.X, 0, (int)chunkVGlobal.Z))
                {
                    continue;
                }

                loadIOQueue.Enqueue(chunkVGlobal);
            }

            MiniProfiler.Instance.EndDebug("Dispatch.Load");

            MiniProfiler.Instance.Begin("Dispatch.Unload");

            MiniProfiler.Instance.Begin("Dispatch.Unload.Lock");
            // Unload chunks
            lock (loadedInternal.SyncRoot)
            {
                MiniProfiler.Instance.EndDebug("Dispatch.Unload.Lock");
                unloadIOQueue.Lock();
                for (int i = 0; i < loadedInternal.Count; i++)
                {
                    ChunkSegment segment = loadedInternal[i];
                    if (!loadedChunksIndices.Contains(segment.Position))
                    {
                        unloadIOQueue.EnqueueUnsafe(segment);
                    }
                    loadedRegionIds.Remove(segment.Position);
                }
                unloadIOQueue.ReleaseLock();
            }

            MiniProfiler.Instance.EndDebug("Dispatch.Unload");

            SignalIOWorkers();

            MiniProfiler.Instance.EndDebug("Dispatch.Total");
        }

        public enum Direction
        {
            None, Left, Right
        }

        private bool first = true;

        public void Dispatch(Vector3 pos)
        {
            if (first)
            {
                first = false;
                DispatchInitial(pos);
                return;
            }
            MiniProfiler.Instance.Clear();
            MiniProfiler.Instance.Begin("Dispatch.Total");
            MiniProfiler.Instance.Begin("Dispatch.Load");
            loadedChunksIndices.Clear();
            unloadChunksIndices.Clear();

            Vector3 delta = lastPos - pos;

            // ignore y axis since we use a 2D plane for chunk loading.
            int dX = (int)delta.X;
            int dY = (int)delta.Z;
            int xAbs = Math.Abs(dX);
            int yAbs = Math.Abs(dY);
            loadIOQueue.Lock();
            if (dX != 0 || dY != 0)
            {
                int dirX = dX > 0 ? -1 : 1;
                int dirY = dY > 0 ? -1 : 1;

                for (int x = 0; x < xAbs; x++) // Process columns along X movement
                {
                    int unloadX = (int)lastPos.X - dirX * (RenderDistance + x); // Move further out based on distance
                    for (int z = (int)lastPos.Z - RenderDistance; z <= (int)lastPos.Z + RenderDistance; z++)
                    {
                        Vector2 chunkToUnload = new(unloadX, z);
                        unloadChunksIndices.Add(chunkToUnload);
                    }
                }

                for (int y = 0; y < yAbs; y++) // Process rows along Y (Z-axis) movement
                {
                    int unloadZ = (int)lastPos.Z - dirY * (RenderDistance + y);
                    for (int x = (int)lastPos.X - RenderDistance; x <= (int)lastPos.X + RenderDistance; x++)
                    {
                        Vector2 chunkToUnload = new(x, unloadZ);
                        unloadChunksIndices.Add(chunkToUnload);
                    }
                }

                for (int x = 0; x < xAbs; x++) // Process columns along X movement
                {
                    int loadX = (int)pos.X + dirX * (RenderDistance - x); // Move closer based on distance
                    for (int z = (int)pos.Z - RenderDistance; z <= (int)pos.Z + RenderDistance; z++)
                    {
                        Vector2 chunkToLoad = new(loadX, z);
                        if (World.InWorldLimits((int)chunkToLoad.X, 0, (int)chunkToLoad.Y) && loadedChunksIndices.Add(chunkToLoad))
                        {
                            Vector3 chunkVGlobal = new(chunkToLoad.X, 0, chunkToLoad.Y);
                            loadIOQueue.EnqueueUnsafe(chunkVGlobal);
                            loadedRegionIds.Add(chunkToLoad);
                        }
                    }
                }

                for (int y = 0; y < yAbs; y++) // Process rows along Y (Z-axis) movement
                {
                    int loadZ = (int)pos.Z + dirY * (RenderDistance - y);
                    for (int x = (int)pos.X - RenderDistance; x <= (int)pos.X + RenderDistance; x++)
                    {
                        Vector2 chunkToLoad = new(x, loadZ);
                        if (World.InWorldLimits((int)chunkToLoad.X, 0, (int)chunkToLoad.Y) && loadedChunksIndices.Add(chunkToLoad))
                        {
                            Vector3 chunkVGlobal = new(chunkToLoad.X, 0, chunkToLoad.Y);
                            loadIOQueue.EnqueueUnsafe(chunkVGlobal);
                            loadedRegionIds.Add(chunkToLoad);
                        }
                    }
                }
            }
            loadIOQueue.ReleaseLock();

            MiniProfiler.Instance.EndDebug("Dispatch.Load");

            MiniProfiler.Instance.Begin("Dispatch.Unload");

            MiniProfiler.Instance.Begin("Dispatch.Unload.Lock");
            // Unload chunks
            lock (loadedInternal.SyncRoot)
            {
                unloadIOQueue.Lock();
                MiniProfiler.Instance.EndDebug("Dispatch.Unload.Lock");
                for (int i = 0; i < loadedInternal.Count; i++)
                {
                    ChunkSegment segment = loadedInternal[i];
                    if (unloadChunksIndices.Contains(segment.Position))
                    {
                        unloadIOQueue.EnqueueUnsafe(segment);
                    }
                    loadedRegionIds.Remove(segment.Position);
                }
                unloadIOQueue.ReleaseLock();
            }

            MiniProfiler.Instance.EndDebug("Dispatch.Unload");

            SignalIOWorkers();

            MiniProfiler.Instance.EndDebug("Dispatch.Total");

            lastPos = pos;
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
        public void Upload(ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context)
        {
            if (uploadQueue.IsEmpty & unloadQueue.IsEmpty)
            {
                return;
            }

            while (uploadQueue.TryDequeue(out var region))
            {
                region.Update(device, context);
            }

            while (integrationQueue.TryDequeue(out ChunkSegment segment))
            {
                loadedChunks.AddRange(segment.Chunks);
            }

            while (unloadQueue.TryDequeue(out ChunkSegment segment))
            {
                for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
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

            segment.Load(pool, true);

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

            segment.Load(pool, true);

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

        private UnsafeHashSet<Vector2> loadedChunksIndices = new(Nucleus.Settings.ChunkRenderDistance * 2 * Nucleus.Settings.ChunkRenderDistance * 2);
        private UnsafeHashSet<Vector2> unloadChunksIndices = new(Nucleus.Settings.ChunkRenderDistance * 2 * Nucleus.Settings.ChunkRenderDistance * 2);

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
                bool signal = false;

                {
                    signal |= !unloadIOQueue.IsEmpty;
                    if (unloadIOQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        UnloadIORegion(segment);
                    }
                }

                {
                    if (saveIOQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        if (!DoNotSave)
                        {
                            segment.SaveToDisk();
                        }
                    }
                }

                {
                    signal |= !loadIOQueue.IsEmpty;
                    if (loadIOQueue.TryDequeue(out Vector3 chunkVGlobal) && running)
                    {
                        ChunkSegment segment = World.GetSegment(chunkVGlobal);
                        LoadIORegion(segment);
                    }
                }

                if (signal)
                {
                    SignalWorkers();
                }

                if (running && unloadIOQueue.IsEmpty && saveIOQueue.IsEmpty && loadIOQueue.IsEmpty)
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