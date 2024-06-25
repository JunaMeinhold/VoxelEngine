namespace VoxelEngine.Voxel
{
    using System;
    using System.Numerics;

    public struct LocalChunkPoint : IEquatable<LocalChunkPoint>
    {
        public byte X;
        public byte Y;
        public byte Z;

        public readonly Vector3 ToGlobal(Chunk chunk)
        {
            return chunk.Position + new Vector3(X, Y, Z);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is LocalChunkPoint point && Equals(point);
        }

        public readonly bool Equals(LocalChunkPoint other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(LocalChunkPoint left, LocalChunkPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalChunkPoint left, LocalChunkPoint right)
        {
            return !(left == right);
        }
    }
}