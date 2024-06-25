namespace VoxelEngine.Voxel
{
    using System.Buffers.Binary;

    public struct ChunkRecord
    {
        public ushort Type;
        public byte X;
        public byte Y;
        public byte Z;
        public byte Count;

        public ChunkRecord(ushort type, byte x, byte y, byte z, byte count)
        {
            Type = type;
            X = x;
            Y = y;
            Z = z;
            Count = count;
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[7]; // Smaller buffer size
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, Type);
            buffer[2] = X;
            buffer[3] = Y;
            buffer[4] = Z;
            buffer[5] = Count;
            stream.Write(buffer);
        }

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[7]; // Match the new struct size
            stream.Read(buffer);
            Type = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            X = buffer[2];
            Y = buffer[3];
            Z = buffer[4];
            Count = buffer[5];
        }
    }
}