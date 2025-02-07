namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Utilities;
    using System.IO;
    using VoxelEngine.IO;

    public static class ChunkSerializer
    {
        public static unsafe ChunkPreSerialized PreSerialize(Chunk* chunk)
        {
            long size = ChunkHeader.Size;

            ChunkPreSerialized result = default;
            var records = result.Runs = [];

            int runsWritten = 0;
            if (chunk->InMemory)
            {
                size += EncodeHeightMap(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED, &result.MinYRuns, &result.CompressionMinY, Chunk.CHUNK_SIZE);
                size += EncodeHeightMap(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED, &result.MaxYRuns, &result.CompressionMaxY, 0);

                size += chunk->BlockMetadata.SizeOf();

                BlockRun run = default;
                bool newRun = true;

                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int zShifted = z << 8;
                    int heightMapAccess = z << 4;
                    for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                    {
                        int y = chunk->MinY[heightMapAccess];
                        int yMax = chunk->MaxY[heightMapAccess];

                        heightMapAccess++;

                        int access = zShifted + (x << 4) + y;
                        Block* voxels = chunk->Data + access;

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
                                records.Add(run);
                                if (runsWritten > ChunkHeader.RLEBreakevenPoint)
                                {
                                    goto end;
                                }
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
                    }
                }

                if (!newRun)
                {
                    runsWritten++;
                    records.Add(run);
                    if (runsWritten > ChunkHeader.RLEBreakevenPoint)
                    {
                        goto end;
                    }
                }
            }

        end:

            ChunkHeader header = default;
            if (records.Count > ChunkHeader.RLEBreakevenPoint)
            {
                result.Compression = ChunkCompression.Raw;
                size += Chunk.CHUNK_SIZE_CUBED * sizeof(ushort);
                records.Release();
            }
            else
            {
                result.Compression = ChunkCompression.RLE;
                size += records.Count * sizeof(BlockRun);
            }

            header.BlockCount = chunk->BlockCount;
            header.Length = size - ChunkHeader.Size;

            result.Chunk = chunk;
            result.Header = header;
            result.Runs = records;
            result.Length = size;

            return result;
        }

        private static unsafe int EncodeHeightMap(byte* values, int length, UnsafeList<HeightMapRun>* output, ChunkCompression* compression, int zeroValue)
        {
            HeightMapRun run = default;
            bool newRun = true;
            int runsWritten = 0;
            for (int i = 0; i < length; i++, values++)
            {
                var value = *values;
                if (value == run.Value && !newRun)
                {
                    run.Count++;
                    continue;
                }

                if (!newRun)
                {
                    runsWritten++;
                    output->Add(run);
                    if (runsWritten > ChunkHeader.RLEHeightMapBreakevenPoint)
                    {
                        goto end;
                    }
                }

                newRun = true; // prevent writing a run when hitting break;
                for (; i < length && *values == zeroValue; i++, values++) ;
                if (i == length) break;

                value = *values;
                run.Value = value;
                run.Count = 0;
                run.Index = (byte)i;
                newRun = false;
            }

            if (!newRun)
            {
                runsWritten++;
                output->Add(run);
            }

        end:

            if (runsWritten > ChunkHeader.RLEHeightMapBreakevenPoint)
            {
                *compression = ChunkCompression.Raw;
                output->Release();
                return Chunk.CHUNK_SIZE_SQUARED;
            }
            else
            {
                *compression = ChunkCompression.RLE;
                return runsWritten * sizeof(BlockRun);
            }
        }

        public static unsafe void Serialize(Chunk* chunk, Stream stream)
        {
            var result = PreSerialize(chunk);
            var header = result.Header;

            header.Write(stream);
            if (chunk->InMemory)
            {
                WriteHeightMap(stream, result.CompressionMinY, chunk->MinY, result.MinYRuns);
                WriteHeightMap(stream, result.CompressionMaxY, chunk->MaxY, result.MaxYRuns);

                chunk->BlockMetadata.Serialize(stream);

                stream.WriteUInt16((ushort)result.Compression);
                switch (result.Compression)
                {
                    case ChunkCompression.Raw:
                        Block* data = chunk->Data;
                        Write(stream, data, Chunk.CHUNK_SIZE_CUBED);
                        break;

                    case ChunkCompression.RLE:
                        stream.WriteUInt16((ushort)result.Runs.Count);
                        Write(stream, result.Runs.Data, result.Runs.Count);
                        break;
                }
            }
            result.Release();
        }

        private static unsafe void WriteHeightMap(Stream stream, ChunkCompression compression, byte* raw, UnsafeList<HeightMapRun> runs)
        {
            stream.WriteUInt16((ushort)compression);
            switch (compression)
            {
                case ChunkCompression.Raw:
                    stream.Write(new ReadOnlySpan<byte>(raw, Chunk.CHUNK_SIZE_SQUARED));
                    break;

                case ChunkCompression.RLE:
                    stream.WriteByte((byte)runs.Count);
                    Write(stream, runs.Data, runs.Count);
                    break;
            }
        }

        private static unsafe void Write<T>(Stream stream, T* data, int length) where T : unmanaged, IBinarySerializable
        {
            if (BitConverter.IsLittleEndian)
            {
                stream.Write(new Span<byte>(data, sizeof(T) * length));
            }
            else
            {
                const int bufferSize = 8192;
                int maxItems = bufferSize / sizeof(T);
                int maxBytes = bufferSize - bufferSize % sizeof(T);

                Span<byte> buffer = stackalloc byte[maxBytes];
                int offset = 0;
                int itemCount = 0;
                T* end = data + length;
                while (data != end)
                {
                    offset += data->Write(buffer[offset..]);
                    itemCount++;
                    if (itemCount >= maxItems)
                    {
                        stream.Write(buffer);
                        offset = 0; itemCount = 0;
                    }
                    data++;
                }
                if (offset > 0)
                {
                    stream.Write(buffer[..offset]);
                }
            }
        }

        private static unsafe void Read<T>(Stream stream, T* data, int length) where T : unmanaged, IBinarySerializable
        {
            if (BitConverter.IsLittleEndian)
            {
                stream.ReadExactly(new Span<byte>(data, sizeof(T) * length));
            }
            else
            {
                const int bufferSize = 8192;
                int remainingBytes = sizeof(T) * length;
                int maxBytes = bufferSize - bufferSize % sizeof(T);

                Span<byte> buffer = stackalloc byte[maxBytes];

                while (remainingBytes > 0)
                {
                    int toRead = Math.Min(maxBytes, remainingBytes);
                    stream.ReadExactly(buffer[..toRead]);
                    remainingBytes -= toRead;
                    int offset = 0;
                    while (offset < toRead)
                    {
                        offset += data->Read(buffer[offset..]);
                        data++;
                    }
                }
            }
        }

        public static unsafe void Deserialize(Chunk* chunk, Stream stream)
        {
            ChunkHeader header = ChunkHeader.ReadFrom(stream);

            if (header.BlockCount == 0)
            {
                stream.Position += header.Length;
                return; // skip reading/allocating.
            }

            if (!chunk->InMemory)
            {
                chunk->Allocate(false);
            }

            ZeroMemoryT(chunk->Data, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED);
            Memset(chunk->MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);

            ReadHeightMap(stream, chunk->MinY);
            ReadHeightMap(stream, chunk->MaxY);

            chunk->BlockMetadata.Deserialize(stream);
            chunk->BlockCount = header.BlockCount;

            ChunkCompression compression = (ChunkCompression)stream.ReadUInt16();
            switch (compression)
            {
                case ChunkCompression.Raw:
                    Read(stream, chunk->Data, Chunk.CHUNK_SIZE_CUBED);
                    break;

                case ChunkCompression.RLE:
                    ushort runCount = stream.ReadUInt16();
                    BlockRun run = default;
                    for (int i = 0; i < runCount; i++)
                    {
                        run.Read(stream);

                        if (run.Count == Chunk.CHUNK_SIZE_CUBED)
                        {
                            MemsetT(chunk->Data, new Block(run.Type), Chunk.CHUNK_SIZE_CUBED);
                            break;
                        }

                        int access = run.Index;
                        int remaining = run.Count;
                        int z = (access >> 8) & 0xF;
                        int x = (access >> 4) & 0xF;
                        int y = access & 0xF;

                        if (access + remaining > Chunk.CHUNK_SIZE_CUBED) throw new IndexOutOfRangeException("Corrupt chunk data, RLE Decoding exceeded chunk boundaries.");

                        int heightMapAccess = (z << 4) + x;

                        Block block = new(run.Type);

                        int minY = chunk->MinY[heightMapAccess];
                        int maxY = chunk->MaxY[heightMapAccess];

                        while (remaining > 0)
                        {
                            int toSet = Math.Min(remaining, maxY - y);
                            MemsetT(chunk->Data + access, block, toSet);
                            remaining -= toSet;

                            if (remaining <= 0)
                            {
                                break;
                            }

                            do
                            {
                                x++;
                                if (x >= Chunk.CHUNK_SIZE)
                                {
                                    x = 0;
                                    z++;
                                    heightMapAccess = z << 4;
                                    if (z == Chunk.CHUNK_SIZE) throw new IndexOutOfRangeException("Corrupt chunk data, RLE Decoding exceeded chunk boundaries.");
                                }
                                else
                                {
                                    heightMapAccess++;
                                }

                                y = chunk->MinY[heightMapAccess];
                                maxY = chunk->MaxY[heightMapAccess];
                                if (y > maxY) continue;
                                access = (z << 8) + (x << 4) + y;
                            } while (y > maxY);
                        }
                    }
                    break;
            }
        }

        private static unsafe void ReadHeightMap(Stream stream, byte* output)
        {
            ChunkCompression compression = (ChunkCompression)stream.ReadUInt16();
            switch (compression)
            {
                case ChunkCompression.Raw:
                    stream.ReadExactly(new Span<byte>(output, Chunk.CHUNK_SIZE_SQUARED));
                    break;

                case ChunkCompression.RLE:
                    int runCount = stream.ReadByte();
                    HeightMapRun run = default;
                    for (int i = 0; i < runCount; i++)
                    {
                        run.Read(stream);

                        if (run.Index + run.Count + 1 > Chunk.CHUNK_SIZE_CUBED) throw new IndexOutOfRangeException("Corrupt height-map data, RLE Decoding exceeded chunk boundaries.");

                        MemsetT(output + run.Index, run.Value, run.Count + 1); // shift count by one to remap from 0..255 to 1..256
                    }
                    break;
            }
        }
    }
}