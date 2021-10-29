using System.Numerics;

namespace HexaEngine.Models.ObjLoader.Loader.Data.VertexData
{
    public struct Texture
    {
        public Texture(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        public float X { get; private set; }
        public float Y { get; private set; }

        public static implicit operator Vector2(Texture texture)
        {
            return new Vector2(texture.X, texture.Y);
        }
    }
}