namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;

    internal interface IVoxelRegionFileInternal
    {
        DateTime LastAccess { get; set; }
        int CurrentLockCount { get; }
        Point2 Id { get; }

        internal void Dispose(bool full);

        internal void Reset(Point2 id, Stream stream, bool newFile);

        internal void Lock();
    }

    public class VoxelRegionFile : IVoxelRegionFileInternal
    {
        private readonly SemaphoreSlim semaphore = new(1);
        private Point2 id;
        private DateTime lastAccess;
        private VoxelRegion chunkRegion = new();
        private Stream Stream;

        public VoxelRegionFile()
        {
            Stream = null!;
        }

        public Point2 Id => id;

        public int CurrentLockCount => semaphore.CurrentCount;

        DateTime IVoxelRegionFileInternal.LastAccess { get => lastAccess; set => lastAccess = value; }

        void IVoxelRegionFileInternal.Dispose(bool full)
        {
            Stream?.Dispose();
            Stream = null!;
            if (full)
            {
                semaphore.Dispose();
            }
        }

        void IVoxelRegionFileInternal.Reset(Point2 id, Stream stream, bool newFile)
        {
            this.id = id;
            Stream = stream;
            lastAccess = DateTime.UtcNow;
            if (newFile)
            {
                stream.Position = 0;
                chunkRegion = new();
                chunkRegion.Serialize(stream);
            }
            else
            {
                stream.Position = 0;
                chunkRegion = default;
                chunkRegion.Deserialize(stream);
            }
        }

        void IVoxelRegionFileInternal.Lock()
        {
            semaphore.Wait();
        }

        public void Dispose(bool write)
        {
            if (write)
            {
                chunkRegion.Flush(Stream);
            }

            semaphore.Release();
        }

        public unsafe void WriteSegment(ChunkSegment* segment)
        {
            Point2 pointInRegion = new(segment->Position.X & 31, segment->Position.Y & 31);
            chunkRegion.WriteSegment(Stream, segment, pointInRegion);
        }

        public unsafe void ReadSegment(World world, ChunkSegment* segment)
        {
            Point2 pointInRegion = new(segment->Position.X & 31, segment->Position.Y & 31);
            chunkRegion.ReadSegment(Stream, world, segment, pointInRegion);
        }

        public bool Exists(ChunkSegment segment)
        {
            Point2 pointInRegion = new(segment.Position.X & 31, segment.Position.Y & 31);
            return chunkRegion.Exists(pointInRegion);
        }
    }
}