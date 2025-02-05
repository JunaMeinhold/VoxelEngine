namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;

    public struct VoxelStorageRegionHeader
    {
    }

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

        private void BeginRead()
        {
            writeLock.Wait();

            readLock.Reset();

            readSemaphore.Wait();
        }

        private void EndRead()
        {
            var value = readSemaphore.Release();
            if (value == maxReader - 1)
            {
                readLock.Set();
            }
        }

        private void BeginWrite()
        {
            readLock.Wait();

            writeLock.Reset();

            writeSemaphore.Wait();
        }

        private void EndWrite()
        {
            var value = writeSemaphore.Release();
            if (value == maxWriter - 1)
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

    public static class ChunkSerializer
    {
        public static unsafe void Serialize(Chunk* chunk, Stream stream)
        {
            long begin = stream.Position;

            stream.Position += ChunkHeader.Size;

            stream.Write(new ReadOnlySpan<byte>(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.Write(new ReadOnlySpan<byte>(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk->BlockMetadata.Serialize(stream);
            chunk->BiomeMetadata.Serialize(stream);

            int runsWritten = 0;
            if (chunk->InMemory)
            {
                for (int k = 0; k < Chunk.CHUNK_SIZE; k++)
                {
                    // Calculate this once, rather than multiple times in the inner loop
                    int kCS2 = k * Chunk.CHUNK_SIZE_SQUARED;

                    int heightMapAccess = k * Chunk.CHUNK_SIZE;

                    for (int i = 0; i < Chunk.CHUNK_SIZE; i++)
                    {
                        // Determine where to start the innermost loop
                        int j = chunk->MinY[heightMapAccess];
                        int topJ = chunk->MaxY[heightMapAccess];
                        heightMapAccess++;

                        // Calculate this once, rather than multiple times in the inner loop
                        int iCS = i * Chunk.CHUNK_SIZE;

                        // Calculate access here and increment it each time in the innermost loop
                        int access = kCS2 + iCS + j;

                        ChunkRecord run = default;
                        bool newRun = true;

                        // X and Z runs search upwards to create runs, so start at the bottom.
                        for (; j < topJ; j++, access++)
                        {
                            Block b = chunk->Data[access];
                            if (newRun || run.Type != b.Type)
                            {
                                if (!newRun)
                                {
                                    runsWritten++;
                                    run.Write(stream);
                                }
                                if (b.Type != Chunk.EMPTY)
                                {
                                    // we could quantize + palette here, but that would add more loading complexity rather than being useful, disk space is more cheap compared to RAM.
                                    run.Type = b.Type;
                                    // max index is 4096 and max value of ushort is 65536 which means we can simply cast it and save one byte instead of storing the position and loading times will be faster.
                                    run.Index = (ushort)access;
                                    run.Count = 1;
                                    newRun = false;
                                }
                            }
                            else if (b.Type != Chunk.EMPTY)
                            {
                                run.Count++;
                            }
                        }

                        if (!newRun)
                        {
                            runsWritten++;
                            run.Write(stream);
                        }
                    }
                }
            }

            long end = stream.Position;
            stream.Position = begin;
            ChunkHeader.Write(stream, runsWritten);
            stream.Position = end;
        }

        public static unsafe void Deserialize(Chunk* chunk, Stream stream)
        {
            if (!chunk->Data.IsAllocated)
            {
                chunk->Data = new(Chunk.CHUNK_SIZE_CUBED);
                chunk->MinY = AllocT<byte>(Chunk.CHUNK_SIZE_SQUARED);
                ZeroMemoryT(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED);
                chunk->MaxY = AllocT<byte>(Chunk.CHUNK_SIZE_SQUARED);
                ZeroMemoryT(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED);
                Memset(chunk->MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);
            }

            ChunkHeader.Read(stream, out int recordCount);

            stream.ReadExactly(new Span<byte>(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.ReadExactly(new Span<byte>(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk->BlockMetadata.Deserialize(stream);
            chunk->BiomeMetadata.Deserialize(stream);

            ChunkRecord record = default;
            for (int i = 0; i < recordCount; i++)
            {
                record.Read(stream);

                for (int y = 0; y < record.Count; y++)
                {
                    int index = record.Index + y;
                    chunk->Data[index] = new Block(record.Type);
                }
            }
        }
    }
}