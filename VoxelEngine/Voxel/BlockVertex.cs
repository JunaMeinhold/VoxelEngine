namespace VoxelEngine.Voxel
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public static class BlockVertex
    {
        public static int[] IndexToTextureShifted { get; set; } = new int[]
        {
            0, 0 << 18, 1 << 18, 2 << 18, 3 << 18, 4 << 18, 5 << 18, 6 << 18, 7 << 18, 8 << 18, 9 << 18, 10 << 18, 11 << 18
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadX(IVoxelVertexBuffer buffer, int x, int yL, int yR, int kL, int kR, int normal, int light)
        {
            int shared = x | light | normal;

            int index = buffer.Count;
            buffer.Increase(6);

            buffer[index + 1] = yR | kL | shared;
            buffer[index] = buffer[index + 4] = yL | kL | shared;
            buffer[index + 2] = buffer[index + 5] = yR | kR | shared;
            buffer[index + 3] = yL | kR | shared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadY(IVoxelVertexBuffer buffer, int xL, int xR, int y, int zL, int zR, int normal, int light)
        {
            int shared = y | light | normal;

            int index = buffer.Count;
            buffer.Increase(6);

            buffer[index + 1] = xL | zR | shared;
            buffer[index] = buffer[index + 4] = xL | zL | shared;
            buffer[index + 2] = buffer[index + 5] = xR | zR | shared;
            buffer[index + 3] = xR | zL | shared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadZ(IVoxelVertexBuffer buffer, int xL, int xR, int yL, int yR, int z, int normal, int light)
        {
            int shared = z | light | normal;

            int index = buffer.Count;
            buffer.Increase(6);

            buffer[index + 1] = xR | yR | shared;
            buffer[index] = buffer[index + 4] = xR | yL | shared;
            buffer[index + 2] = buffer[index + 5] = xL | yR | shared;
            buffer[index + 3] = xL | yL | shared;
        }
    }
}