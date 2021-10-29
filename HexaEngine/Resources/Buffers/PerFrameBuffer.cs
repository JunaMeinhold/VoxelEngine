using System.Numerics;
using System.Runtime.InteropServices;

namespace HexaEngine.Resources.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PerFrameBuffer
    {
        public Matrix4x4 World;
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerFrameBuffer2
    {
        public Matrix4x4 MVP;
    }
}