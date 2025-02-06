namespace VoxelEngine.Voxel.Serialization
{
    using System.IO;

    public static class ChunkSerializer
    {
        public static unsafe void Serialize(Chunk* chunk, Stream stream)
        {
            long begin = stream.Position;

            stream.Position += ChunkHeader.Size;

            int runsWritten = 0;
            if (chunk->InMemory)
            {
                stream.Write(new ReadOnlySpan<byte>(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED));
                stream.Write(new ReadOnlySpan<byte>(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED));

                chunk->BlockMetadata.Serialize(stream);

                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int zShifted = z << 8;
                    int heightMapAccess = z * Chunk.CHUNK_SIZE;
                    for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                    {
                        int y = chunk->MinY[heightMapAccess];
                        int yMax = chunk->MaxY[heightMapAccess];

                        heightMapAccess++;

                        int access = zShifted + (x << 4) + y;
                        Block* voxels = chunk->Data + access;

                        ChunkRecord run = default;
                        bool newRun = true;

                        for (; y < yMax; y++, access++, voxels++)
                        {
                            Block b = *voxels;

                            if (b.Type == run.Type && !newRun)
                            {
                                run.Count++;
                                continue;
                            }

                            if (!newRun)
                            {
                                runsWritten++;
                                run.Write(stream);
                            }

                            newRun = true; // prevent writing a run when hitting break;
                            for (; y < yMax && voxels->Type == Chunk.EMPTY; y++, access++, voxels++) ;
                            if (y == yMax) break;

                            b = *voxels;
                            run.Type = b.Type;
                            run.Count = 1;
                            run.Index = (ushort)access;
                            newRun = false;
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
            ChunkHeader.Write(stream, runsWritten, end - begin - ChunkHeader.Size);
            stream.Position = end;
        }

        public static unsafe void Deserialize(Chunk* chunk, Stream stream)
        {
            ChunkHeader.Read(stream, out int recordCount, out long length);

            if (recordCount == 0)
            {
                stream.Position += length;
                return; // skip reading/allocating.
            }

            if (!chunk->InMemory)
            {
                chunk->Allocate(false);
            }

            ZeroMemoryT(chunk->Data, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED);
            Memset(chunk->MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);

            stream.ReadExactly(new Span<byte>(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED));
            stream.ReadExactly(new Span<byte>(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED));

            chunk->BlockMetadata.Deserialize(stream);

            ChunkRecord record = default;
            for (int i = 0; i < recordCount; i++)
            {
                record.Read(stream);
                MemsetT(chunk->Data + record.Index, new Block(record.Type), record.Count);
                chunk->BlockCount += record.Count;
            }
        }
    }
}