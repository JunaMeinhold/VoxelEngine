namespace VoxelEngine.Mathematics
{
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D.Attributes;

    [StructLayout(LayoutKind.Sequential)]
    [PerVertexData]
    public struct LineVertex
    {
        public LineVertex(Vector4 position)
        {
            Position = position;
        }

        [SemanticName("POSITION")]
        [SemanticIndex(0)]
        [Offset(0)]
        [Format(Format.R32G32B32A32_Float)]
        public Vector4 Position;
    }
}