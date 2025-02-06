namespace VoxelEngine.Voxel
{
    using VoxelEngine.IO;

    public readonly struct ChunkHeader
    {
        public static readonly Version Version = new(1, 0, 0, 0);
        public static readonly Version MinVersion = new(1, 0, 0, 0);

        public const int Size = 16;

        public static void Write(Stream stream, int recordCount, long length)
        {
            stream.WriteUInt32(Version);
            stream.WriteInt32(recordCount);
            stream.WriteInt64(length);
        }

        public static void Read(Stream stream, out int recordCount, out long length)
        {
            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }

            stream.ReadInt32(out recordCount);
            stream.ReadInt64(out length);
        }

        public static bool TryRead(Stream stream, out int recordCount, out long length)
        {
            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                recordCount = -1;
                length = 0;
                return false;
            }

            stream.ReadInt32(out recordCount);
            stream.ReadInt64(out length);
            return true;
        }
    }
}