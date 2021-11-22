using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HexaEngine.Objects.VoxelGen
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public static class BlockVertex
    {
        public static int[] IndexToTextureShifted { get; set; } = new int[]
        {
            0, 0 << 18, 1 << 18, 2 << 18
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadX(BlockVertexBuffer buffer, int x, int yL, int yR, int kL, int kR, int normal, int light)
        {
            var shared = x | light | normal;

            buffer.Data[buffer.Used + 1] = yR | kL | shared;
            buffer.Data[buffer.Used] = buffer.Data[buffer.Used + 4] = yL | kL | shared;
            buffer.Data[buffer.Used + 2] = buffer.Data[buffer.Used + 5] = yR | kR | shared;
            buffer.Data[buffer.Used + 3] = yL | kR | shared;

            buffer.Used += 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadY(BlockVertexBuffer buffer, int xL, int xR, int y, int zL, int zR, int normal, int light)
        {
            var shared = y | light | normal;

            buffer.Data[buffer.Used + 1] = xL | zR | shared;
            buffer.Data[buffer.Used] = buffer.Data[buffer.Used + 4] = xL | zL | shared;
            buffer.Data[buffer.Used + 2] = buffer.Data[buffer.Used + 5] = xR | zR | shared;
            buffer.Data[buffer.Used + 3] = xR | zL | shared;

            buffer.Used += 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendQuadZ(BlockVertexBuffer buffer, int xL, int xR, int yL, int yR, int z, int normal, int light)
        {
            var shared = z | light | normal;

            buffer.Data[buffer.Used + 1] = xR | yR | shared;
            buffer.Data[buffer.Used] = buffer.Data[buffer.Used + 4] = xR | yL | shared;
            buffer.Data[buffer.Used + 2] = buffer.Data[buffer.Used + 5] = xL | yR | shared;
            buffer.Data[buffer.Used + 3] = xL | yL | shared;

            buffer.Used += 6;
        }
    }
}