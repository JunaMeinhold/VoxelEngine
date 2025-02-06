namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;

    public class VoxelStorageRegionFile
    {
        public Point2 Id;
        public Stream Stream;
        public DateTime LastAccess;
        public SemaphoreSlim Semaphore = new(1);

        public VoxelStorageRegionFile()
        {
            Stream = null!;
        }

        internal void Reset(Point2 id, Stream stream)
        {
            Id = id;
            Stream = stream;
            LastAccess = DateTime.UtcNow;
        }

        internal void Lock()
        {
            Semaphore.Wait();
        }

        public void ReleaseLock()
        {
            Semaphore.Release();
        }

        public void Return()
        {
            ReleaseLock();
        }
    }
}