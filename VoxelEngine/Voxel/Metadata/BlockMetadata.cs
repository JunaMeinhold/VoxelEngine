namespace VoxelEngine.Voxel.Metadata
{
    using System.Buffers.Binary;

    public class BlockMetadata
    {
        public LocalChunkPoint Position;
        public BlockMetadataType Type;
        public IBlockMetadata Data;

        public int SizeOf()
        {
            return 3 + 4 + Data.SizeOf();
        }

        public void Write(Span<byte> destination)
        {
            destination[0] = Position.X;
            destination[1] = Position.Y;
            destination[2] = Position.Z;
            BinaryPrimitives.WriteInt32LittleEndian(destination[3..], (int)Type);
            Data.Write(destination[7..]);
        }

        public void Read(ReadOnlySpan<byte> source)
        {
            Position.X = source[0];
            Position.Y = source[1];
            Position.Z = source[2];
            Type = (BlockMetadataType)BinaryPrimitives.ReadInt32LittleEndian(source[3..]);
            Data = BlockMetadataFactory.CreateInstance(Type);
            Data.Read(source[7..]);
        }

        public static BlockMetadata ReadFrom(ReadOnlySpan<byte> source)
        {
            BlockMetadata metadata = new();
            metadata.Read(source);
            return metadata;
        }
    }
}