namespace VoxelEngine.Voxel
{
    using System.Numerics;
    using System.Runtime.InteropServices;
    using VoxelEngine.IO;

    public static class ChunkSerializer
    {
        public static unsafe int Serialize(Stream stream, Chunk chunk)
        {
            long begin = stream.Position;

            stream.Position += ChunkHeader.Size;

            stream.Write(new ReadOnlySpan<byte>(chunk.MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.Write(new ReadOnlySpan<byte>(chunk.MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk.BlockMetadata.Serialize(stream);
            chunk.BiomeMetadata.Serialize(stream);

            int blocksWritten = 0;
            if (chunk.Data != null)
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
                            ref Block b = ref chunk.Data[access];

                            if (newRun && b.Type != Chunk.EMPTY)
                            {
                                run.Type = b.Type;
                                run.Position = new(i, j, k);
                                run.Count = 1;
                                newRun = false;
                            }
                            else if (run.Type == b.Type && b.Type != Chunk.EMPTY)
                            {
                                run.Count++;
                            }
                            else
                            {
                                blocksWritten++;

                                if (b.Type != Chunk.EMPTY)
                                {
                                    run.Type = b.Type;
                                    run.Position = new(i, j, k);
                                    run.Count = 1;
                                }
                                else
                                {
                                    newRun = true;
                                }

                                run.Write(stream);
                            }
                        }

                        if (!newRun)
                        {
                            blocksWritten++;
                            run.Write(stream);
                        }
                    }
                }
            }

            long end = stream.Position;
            stream.Position = begin;
            ChunkHeader.Write(stream, blocksWritten);
            stream.Position = end;

            return (int)(end - begin);
        }

        public static unsafe int Deserialize(Chunk chunk, byte* data, int length)
        {
            Span<byte> span = new(data, length);

            if (chunk.Data is null)
            {
                chunk.Data = AllocTAndZero<Block>(Chunk.CHUNK_SIZE_CUBED);
                chunk.MinY = AllocTAndZero<byte>(Chunk.CHUNK_SIZE_SQUARED);
                chunk.MaxY = AllocTAndZero<byte>(Chunk.CHUNK_SIZE_SQUARED);
                Memset(chunk.MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);
            }

            int index = ChunkHeader.Read(span, out int recordCount);

            Memcpy(data + index, chunk.MinY, Chunk.CHUNK_SIZE_SQUARED);
            index += Chunk.CHUNK_SIZE_SQUARED;
            Memcpy(data + index, chunk.MaxY, Chunk.CHUNK_SIZE_SQUARED);
            index += Chunk.CHUNK_SIZE_SQUARED;

            index += chunk.BlockMetadata.Deserialize(span[index..]);
            index += chunk.BiomeMetadata.Deserialize(span[index..]);

            ChunkRecord* records = (ChunkRecord*)(data + index);
            for (int i = 0; i < recordCount; i++, records++)
            {
                ChunkRecord record = *records;
                for (int y = 0; y < record.Count; y++)
                {
                    Vector3 pos = record.Position;
                    pos.Y += y;
                    chunk.Data[pos.MapToIndex(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)] = new Block(record.Type);
                }
            }

            return index + recordCount * sizeof(ChunkRecord);
        }

        public static unsafe void Deserialize(Chunk chunk, Stream stream)
        {
            if (chunk.Data is null)
            {
                chunk.Data = AllocTAndZero<Block>(Chunk.CHUNK_SIZE_CUBED);
                chunk.MinY = AllocTAndZero<byte>(Chunk.CHUNK_SIZE_SQUARED);
                chunk.MaxY = AllocTAndZero<byte>(Chunk.CHUNK_SIZE_SQUARED);
                Memset(chunk.MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);
            }

            ChunkHeader.Read(stream, out int recordCount);

            stream.Read(new Span<byte>(chunk.MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.Read(new Span<byte>(chunk.MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk.BlockMetadata.Deserialize(stream);
            chunk.BiomeMetadata.Deserialize(stream);

            ChunkRecord record = default;
            for (int i = 0; i < recordCount; i++)
            {
                int left = recordCount - i;

                record.Read(stream);

                for (int y = 0; y < record.Count; y++)
                {
                    Vector3 pos = record.Position;
                    pos.Y += y;
                    chunk.Data[pos.MapToIndex(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)] = new Block(record.Type);
                }
            }
        }
    }
}