namespace VoxelEngine.Voxel
{
    using VoxelEngine.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public readonly struct ChunkRegionHeader
    {
        public static readonly byte[] MagicNumber = [0x56, 0x78, 0x6C, 0x52, 0x67, 0x6F, 0x0];
        public static readonly Version Version = new(1, 0, 0, 0);
        public static readonly Version MinVersion = new(1, 0, 0, 0);

        public const int Size = 21;

        public static void Write(Stream stream, int recordCount)
        {
            stream.Write(MagicNumber);
            stream.WriteUInt32(Version);
            stream.WriteInt32(recordCount);
        }

        public static void Read(Stream stream, out int recordCount)
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

            stream.ReadInt32(out recordCount);
        }
    }

    public struct ChunkRegion
    {
        public const int CHUNK_REGION_SIZE = 8;
        public const int CHUNK_REGION_SIZE_SQUARED = 8 * 8;

        public Vector2 Position;
        public ChunkRegionSeekTable SeekTable;

        [InlineArray(CHUNK_REGION_SIZE_SQUARED)]
        public struct ChunkRegionSeekTable
        {
            private long _element0;

            public Span<long> AsSpan()
            {
                return MemoryMarshal.CreateSpan(ref _element0, CHUNK_REGION_SIZE_SQUARED);
            }
        }

        public void Serialize(Stream stream, ChunkSegment segment)
        {
            long start = stream.Position;
            stream.Position += ChunkRegionHeader.Size;
        }

        public void Deserialize(Stream stream)
        {
        }
    }
}