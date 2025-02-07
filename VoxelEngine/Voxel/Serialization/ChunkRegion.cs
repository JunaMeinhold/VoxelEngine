namespace VoxelEngine.Voxel.Serialization
{
    using VoxelEngine.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using VoxelEngine.Voxel;
    using Hexa.NET.Mathematics;
    using System.Buffers.Binary;

    public readonly struct ChunkRegionHeader
    {
        public static readonly byte[] MagicNumber = [0x56, 0x78, 0x6C, 0x52, 0x67, 0x6F, 0x0];
        public static readonly Version Version = new(1, 0, 0, 0);
        public static readonly Version MinVersion = new(1, 0, 0, 0);

        public const int Size = 21;

        public static void Write(Stream stream)
        {
            stream.Write(MagicNumber);
            stream.WriteUInt32(Version);
        }

        public static void Read(Stream stream)
        {
            if (!stream.ReadCompare(MagicNumber))
            {
                throw new FormatException("Invalid magic number");
            }

            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }
        }
    }

    public struct ChunkRegion
    {
        public const int CHUNK_REGION_SIZE = 32;
        public const int CHUNK_REGION_SIZE_SQUARED = 32 * 32;
        public const int SECTOR_SIZE = 4096;

        public Point2 Position;
        public ChunkRegionSeekTable SeekTable;

        [InlineArray(CHUNK_REGION_SIZE_SQUARED)]
        public struct ChunkRegionSeekTable
        {
            private ChunkRegionSeekTableEntry _element0;

            public Span<ChunkRegionSeekTableEntry> AsSpan()
            {
                return MemoryMarshal.CreateSpan(ref _element0, CHUNK_REGION_SIZE_SQUARED);
            }
        }

        public struct ChunkRegionSeekTableEntry
        {
            public long Position;
            public int Count;

            public const int Size = 12;

            public void Read(Stream stream)
            {
                Span<byte> buffer = stackalloc byte[Size];
                stream.ReadExactly(buffer);
                Position = BinaryPrimitives.ReadInt64LittleEndian(buffer);
                Count = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]);
            }

            public readonly void Write(Stream stream)
            {
                Span<byte> buffer = stackalloc byte[Size];
                BinaryPrimitives.WriteInt64LittleEndian(buffer, Position);
                BinaryPrimitives.WriteInt32LittleEndian(buffer, Count);
                stream.Write(buffer);
            }
        }

        public readonly void Serialize(Stream stream)
        {
            ChunkRegionHeader.Write(stream);
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                SeekTable[i].Write(stream);
            }
        }

        public void Deserialize(Stream stream)
        {
            ChunkRegionHeader.Read(stream);
            for (int i = 0; i < CHUNK_REGION_SIZE_SQUARED; i++)
            {
                SeekTable[i].Read(stream);
            }

            ChunkRegionSeekTableEntry entry = SeekTable[0];
        }
    }
}