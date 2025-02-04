namespace VoxelEngine.Voxel
{
    using System.Numerics;

    public static class ChunkSerializer
    {
        public static unsafe void Serialize(Stream stream, Chunk chunk)
        {
            long begin = stream.Position;

            stream.Position += ChunkHeader.Size;

            stream.Write(new ReadOnlySpan<byte>(chunk.MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.Write(new ReadOnlySpan<byte>(chunk.MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk.BlockMetadata.Serialize(stream);
            chunk.BiomeMetadata.Serialize(stream);

            int runsWritten = 0;
            if (chunk.InMemory)
            {
                for (int k = 0; k < Chunk.CHUNK_SIZE; k++)
                {
                    // Calculate this once, rather than multiple times in the inner loop
                    int kCS2 = k * Chunk.CHUNK_SIZE_SQUARED;

                    int heightMapAccess = k * Chunk.CHUNK_SIZE;

                    for (int i = 0; i < Chunk.CHUNK_SIZE; i++)
                    {
                        // Determine where to start the innermost loop
                        int j = chunk.MinY[heightMapAccess];
                        int topJ = chunk.MaxY[heightMapAccess];
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
                            Block b = chunk.Data[access];
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

        public static unsafe void Deserialize(Chunk chunk, Stream stream)
        {
            if (!chunk.Data.IsAllocated)
            {
                chunk.Data = new(Chunk.CHUNK_SIZE_CUBED);
                chunk.MinY = AllocT<byte>(Chunk.CHUNK_SIZE_SQUARED);
                ZeroMemoryT(chunk.MinY, Chunk.CHUNK_SIZE_SQUARED);
                chunk.MaxY = AllocT<byte>(Chunk.CHUNK_SIZE_SQUARED);
                ZeroMemoryT(chunk.MaxY, Chunk.CHUNK_SIZE_SQUARED);
                Memset(chunk.MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);
            }

            ChunkHeader.Read(stream, out int recordCount);

            stream.ReadExactly(new Span<byte>(chunk.MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.ReadExactly(new Span<byte>(chunk.MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk.BlockMetadata.Deserialize(stream);
            chunk.BiomeMetadata.Deserialize(stream);

            ChunkRecord record = default;
            for (int i = 0; i < recordCount; i++)
            {
                record.Read(stream);

                for (int y = 0; y < record.Count; y++)
                {
                    int index = record.Index + y;
                    chunk.Data[index] = new Block(record.Type);
                }
            }
        }
    }
}