namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;
    using System.Runtime.CompilerServices;

    public class VoxelStorageRegionManager
    {
        private const int maxReader = 32;
        private const int maxWriter = 1;

        private readonly ManualResetEventSlim writeLock = new(true);
        private readonly ManualResetEventSlim readLock = new(true);
        private readonly SemaphoreSlim readSemaphore = new(maxReader);
        private readonly SemaphoreSlim writeSemaphore = new(maxWriter);

        private readonly Dictionary<Point2, VoxelStorageRegionFile> idToRegions = new();
        private readonly List<VoxelStorageRegionFile> regions = new();

        public int MaxOpenFiles { get; private set; } = 32;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRead()
        {
            writeLock.Wait();
            readLock.Reset();
            readSemaphore.Wait();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndRead()
        {
            if (readSemaphore.Release() == maxReader - 1)
            {
                readLock.Set();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginWrite()
        {
            readLock.Wait();
            writeLock.Reset();
            writeSemaphore.Wait();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndWrite()
        {
            if (writeSemaphore.Release() == maxWriter - 1)
            {
                writeLock.Set();
            }
        }

        public VoxelStorageRegionFile? AcquireRegionStream(Point2 regionPos, string worldPath, bool create)
        {
            BeginRead();

            try
            {
                if (idToRegions.TryGetValue(regionPos, out VoxelStorageRegionFile? region))
                {
                    region.Lock();
                    region.LastAccess = DateTime.UtcNow;
                    return region;
                }
            }
            finally
            {
                EndRead();
            }

            BeginWrite();
            try
            {
                if (!idToRegions.TryGetValue(regionPos, out var region))
                {
                    TryEvict(out region);
                    string filename = Path.Combine(worldPath, $"r.{regionPos.X}.{regionPos.Y}.vxr");
                    if (!File.Exists(filename) && !create)
                    {
                        return null;
                    }

                    FileStream stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                    if (region is null)
                    {
                        region = new();

                        regions.Add(region);
                    }

                    region.Reset(regionPos, stream);

                    idToRegions[regionPos] = region;
                }

                return region;
            }
            finally
            {
                EndWrite();
            }
        }

        private void TryEvict(out VoxelStorageRegionFile? old)
        {
            if (idToRegions.Count < MaxOpenFiles)
            {
                old = null;
                return;
            }

            VoxelStorageRegionFile? leastUsed = null;

            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (region.Semaphore.CurrentCount == 1)
                {
                    if (leastUsed == null || region.LastAccess < leastUsed.LastAccess)
                    {
                        leastUsed = region;
                    }
                }
            }

            if (leastUsed != null)
            {
                leastUsed.Stream.Dispose();
                idToRegions.Remove(leastUsed.Id);
            }

            old = leastUsed;
        }
    }
}