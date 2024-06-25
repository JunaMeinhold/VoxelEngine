namespace VoxelEngine.Voxel.Metadata
{
    public interface IBlockMetadata
    {
        public BlockMetadataType Type { get; }

        public int SizeOf();

        public void Write(Span<byte> destination);

        public void Read(ReadOnlySpan<byte> source);
    }
}