namespace VoxelEngine.Voxel.Blocks
{
    using VoxelEngine.Voxel;

    public struct BlockEntry
    {
        public byte Id;
        public string Name;
        public BlockTextureDescription Description;

        public BlockEntry(string name, BlockTextureDescription description)
        {
            Id = 0;
            Name = name;
            Description = description;
        }

        public static implicit operator Block(BlockEntry entry)
        {
            return new() { Type = entry.Id };
        }
    }
}