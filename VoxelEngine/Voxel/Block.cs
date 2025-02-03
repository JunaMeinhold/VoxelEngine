namespace VoxelEngine.Voxel
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct Block : IEquatable<Block>
    {
        public ushort Type;

        public Block(ushort type)
        {
            Type = type;
        }

        public static readonly Block Air = new(0);

        public static implicit operator ushort(Block block) => block.Type;

        public static implicit operator Block(ushort type) => new(type);

        public override bool Equals(object obj)
        {
            return obj is Block block && Equals(block);
        }

        public readonly bool Equals(Block other)
        {
            return Type == other.Type;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Type);
        }

        public static bool operator ==(Block left, Block right)
        {
            return left.Type == right.Type;
        }

        public static bool operator !=(Block left, Block right)
        {
            return !(left == right);
        }
    }
}