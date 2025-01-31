namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Runtime.CompilerServices;

    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapToIndex(this Point2 vector)
        {
            return vector.X + (vector.Y << 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapToIndex(this Point3 vector)
        {
            return (vector.Z << 8) + (vector.X << 4) + vector.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapToIndex(int x, int y, int z)
        {
            return (z << 8) + (x << 4) + y;
        }
    }
}