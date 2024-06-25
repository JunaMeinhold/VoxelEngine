namespace VoxelEngine.IO.ObjLoader.Data.VertexData
{
    using System.Numerics;

    public struct Vertex
    {
        public Vertex(float x, float y, float z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }

        public static implicit operator Vector4(Vertex vertex)
        {
            return new Vector4(vertex.X, vertex.Y, vertex.Z, 1);
        }
    }
}