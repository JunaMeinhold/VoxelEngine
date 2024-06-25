namespace VoxelEngine.Voxel
{
    using System.Buffers.Binary;
    using System.Numerics;

    public struct ChunkRecord
    {
        public ushort Type;
        public Vector3 Position;
        public byte Count;

        public ChunkRecord(ushort type, Vector3 position)
        {
            Type = type;
            Position = position;
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[15];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, Type);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[2..], Position.X);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[6..], Position.Y);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[10..], Position.Z);
            buffer[14] = Count;
            stream.Write(buffer);
        }

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[15];
            stream.Read(buffer);
            Type = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            Position.X = BinaryPrimitives.ReadSingleLittleEndian(buffer[2..]);
            Position.Y = BinaryPrimitives.ReadSingleLittleEndian(buffer[6..]);
            Position.Z = BinaryPrimitives.ReadSingleLittleEndian(buffer[10..]);
            Count = buffer[14];
        }
    }
}