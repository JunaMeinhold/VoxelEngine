namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;
    using System.IO;

    public struct VoxelRegion
    {
        public const int CHUNK_REGION_SIZE = 32;
        public const int CHUNK_REGION_SIZE_SQUARED = 32 * 32;
        public const int BlockSize = 8192;
        public Point2 Position;
        public VoxelRegionSeekTable SeekTable;

        private long blockStart;
        private int blockCount;

        public VoxelRegion()
        {
            SeekTable.AsSpan().Fill(new() { BlockPosition = -1, Count = 0 });
        }

        public void Serialize(Stream stream)
        {
            ChunkRegionHeader.Write(stream);
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                SeekTable[i].Write(stream);
            }
            blockStart = stream.Position;
        }

        public void Deserialize(Stream stream)
        {
            blockCount = 0;
            ChunkRegionHeader.Read(stream);
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                SeekTable[i].Read(stream);
                blockCount += SeekTable[i].Count;
            }
            blockStart = stream.Position;
        }

        public readonly unsafe bool ReadSegment(Stream stream, World world, ChunkSegment* segment, Point2 point)
        {
            int index = (point.Y << 5) + point.X;
            var entry = SeekTable[index];

            if (entry.BlockPosition == -1) return false;

            stream.Position = blockStart + entry.BlockPosition * BlockSize;
            segment->LoadFromStream(stream, world);

            return true;
        }

        public unsafe void WriteSegment(Stream stream, ChunkSegment* segment, Point2 point)
        {
            int index = (point.Y << 5) + point.X;
            var entry = SeekTable[index];

            Span<ChunkPreSerialized> serializeds = stackalloc ChunkPreSerialized[ChunkSegment.CHUNK_SEGMENT_SIZE];
            long size = segment->PreSerialize(serializeds);

            int requiredBlocks = (int)Math.Ceiling(size / (float)BlockSize);

            bool moved = entry.BlockPosition != -1 && entry.Count != requiredBlocks;

            if (moved)
            {
                MoveEntires(stream, index, entry.BlockPosition, entry.Count);
            }

            if (moved || entry.BlockPosition == -1)
            {
                entry.BlockPosition = blockCount;
                entry.Count = requiredBlocks;
                blockCount += requiredBlocks;
            }

            stream.Flush();
            stream.Position = blockStart + entry.BlockPosition * BlockSize;
            segment->Serialize(stream, serializeds);

            stream.SetLength(blockStart + blockCount * BlockSize);
            SeekTable[index] = entry;
        }

        private void MoveEntires(Stream stream, int index, int position, int count)
        {
            stream.Flush();
            Span<byte> buffer = stackalloc byte[BlockSize];
            long readerPosition = blockStart + position * BlockSize + count * BlockSize;
            long writerPosition = blockStart + position * BlockSize;
            int toMove = blockCount - position - count;

            for (int i = 0; i < toMove; i++)
            {
                stream.Position = readerPosition;
                stream.ReadExactly(buffer);
                readerPosition += BlockSize;
                stream.Position = writerPosition;
                stream.Write(buffer);
                writerPosition += BlockSize;
            }
            blockCount -= count;

            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                var entry = SeekTable[i];

                if (entry.BlockPosition > position)
                {
                    entry.BlockPosition -= count;
                    SeekTable[i] = entry;
                }
            }
        }

        public void Flush(Stream stream)
        {
            stream.Flush();
            stream.Position = 0;
            Serialize(stream);
        }

        public readonly bool Exists(Point2 point)
        {
            int index = (point.Y << 5) + point.X;
            var entry = SeekTable[index];

            return entry.BlockPosition != -1 && entry.Count != 0;
        }
    }
}