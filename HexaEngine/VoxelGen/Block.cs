using System.Runtime.CompilerServices;

namespace VoxelGen
{
    public struct Block
    {
        public byte kind;
        public byte health;
        public int index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is Block b)
            {
                return b.index != index || b.health != health;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Block left, Block right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Block left, Block right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return index.GetHashCode() + health.GetHashCode();
        }
    }
}