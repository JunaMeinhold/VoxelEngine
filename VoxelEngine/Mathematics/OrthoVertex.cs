namespace VoxelEngine.Mathematics
{
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D.Attributes;

    [StructLayout(LayoutKind.Sequential)]
    public struct OrthoVertex
    {
        [SemanticName("POSITION")]
        [SemanticIndex(0)]
        [Offset(0)]
        [Format(Format.R32G32B32_Float)]
        public Vector3 Position;

        [SemanticName("TEXCOORD")]
        [SemanticIndex(0)]
        [Offset(-1)]
        [Format(Format.R32G32_Float)]
        public Vector2 Texture;

        public OrthoVertex(Vector3 position, Vector2 texture)
        {
            Position = position;
            Texture = texture;
        }
    }
}