namespace VoxelEngine.Voxel.Metadata
{
    using System.Buffers.Binary;
    using VoxelEngine.IO;

    public struct BiomeMetadataHeader
    {
        public static readonly Version Version = new(1, 0, 0, 0);
        public static readonly Version MinVersion = new(1, 0, 0, 0);

        public const int Size = 8;

        public static void Write(Stream stream, int dataLength)
        {
            stream.WriteUInt32(Version);
            stream.WriteInt32(dataLength);
        }

        public static void Read(Stream stream, out int dataLength)
        {
            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }

            stream.ReadInt32(out dataLength);
        }

        public static bool TryRead(Stream stream, out int dataLength)
        {
            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                dataLength = -1;
                return false;
            }

            stream.ReadInt32(out dataLength);
            return true;
        }

        public static int Read(ReadOnlySpan<byte> data, out int dataLength)
        {
            Version version = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }

            dataLength = BinaryPrimitives.ReadInt32LittleEndian(data);

            return 8;
        }

        public static bool TryRead(ReadOnlySpan<byte> data, out int read, out int dataLength)
        {
            Version version = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (version > Version || version < MinVersion)
            {
                dataLength = -1;
                read = 4;
                return false;
            }

            dataLength = BinaryPrimitives.ReadInt32LittleEndian(data);
            read = 8;
            return true;
        }
    }
}