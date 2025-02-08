namespace VoxelEngine.Voxel.Serialization
{
    using System.Buffers.Binary;
    using System.IO;

    public struct VoxelRegionSeekTableEntry
    {
        public int BlockPosition;
        public int Count;

        public const int Size = 8;

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[Size];
            stream.ReadExactly(buffer);
            BlockPosition = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            Count = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]);
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[Size];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BlockPosition);
            BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Count);
            stream.Write(buffer);
        }
    }
}