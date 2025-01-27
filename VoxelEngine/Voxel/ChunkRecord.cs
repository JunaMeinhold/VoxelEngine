namespace VoxelEngine.Voxel
{
    using System.Buffers.Binary;

    public struct ChunkRecord
    {
        public ushort Type;
        public ushort Index;
        public byte Count;

        public ChunkRecord(ushort type, ushort index, byte count)
        {
            Type = type;
            Index = index;
            Count = count;
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[5];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, Type);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], Index);
            buffer[4] = Count;
            stream.Write(buffer);
        }

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[5];
            stream.ReadExactly(buffer);
            Type = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            Index = BinaryPrimitives.ReadUInt16LittleEndian(buffer[2..]);
            Count = buffer[4];
        }
    }
}