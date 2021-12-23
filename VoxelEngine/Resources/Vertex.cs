using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace HexaEngine.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector4 Position;
        public Vector3 Texture;
        public Vector3 Normal;

        public void AddPosition(Vector4 vector)
        {
            Position += vector;
        }

        public void AddPosition(Vector3 vector3)
        {
            AddPosition(new Vector4(vector3, 0));
        }

        public void InvertTexture()
        {
            Texture.X = MathF.Abs(Texture.X - 1);
            Texture.Y = MathF.Abs(Texture.Y - 1);
        }

        public Vertex(Vector4 position, Vector2 texture, Vector3 normal)
        {
            Position = position;
            Texture = new Vector3(texture, 0);
            Normal = normal;
        }

        public Vertex(Vector4 position, Vector3 texture, Vector3 normal)
        {
            Position = position;
            Texture = texture;
            Normal = normal;
        }
    }
}