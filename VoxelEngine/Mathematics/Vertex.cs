namespace VoxelEngine.Mathematics
{
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D.Attributes;

    [StructLayout(LayoutKind.Sequential)]
    [PerVertexData]
    public struct Vertex
    {
        [SemanticName("POSITION")]
        [SemanticIndex(0)]
        [Offset(0)]
        [Format(Format.R32G32B32A32_Float)]
        public Vector4 Position;

        [SemanticName("TEXCOORD")]
        [SemanticIndex(0)]
        [Offset(-1)]
        [Format(Format.R32G32B32_Float)]
        public Vector3 Texture;

        [SemanticName("NORMAL")]
        [SemanticIndex(0)]
        [Offset(-1)]
        [Format(Format.R32G32B32_Float)]
        public Vector3 Normal;

        [SemanticName("TANGENT")]
        [SemanticIndex(0)]
        [Offset(-1)]
        [Format(Format.R32G32B32_Float)]
        [SemanticOptional]
        public Vector3 Tangent;

        public void InvertTexture()
        {
            Texture.X = MathF.Abs(Texture.X);
            Texture.Y = MathF.Abs(Texture.Y - 1);
        }

        public Vertex(Vector4 position, Vector2 texture, Vector3 normal)
        {
            Position = position;
            Texture = new Vector3(texture, 0);
            Normal = normal;
            Tangent = Vector3.Zero;
        }

        public Vertex(Vector4 position, Vector2 texture, Vector3 normal, Vector3 tangent)
        {
            Position = position;
            Texture = new Vector3(texture, 0);
            Normal = normal;
            Tangent = tangent;
        }

        public Vertex(Vector4 position, Vector3 texture, Vector3 normal, Vector3 tangent)
        {
            Position = position;
            Texture = texture;
            Normal = normal;
            Tangent = tangent;
        }

        public Vertex(Vertex vertex, Vector3 normal, Vector3 tangent)
        {
            Position = vertex.Position;
            Texture = vertex.Texture;
            Normal = normal;
            Tangent = tangent;
        }
    }
}