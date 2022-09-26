namespace VoxelEngine.Objects.Primitives
{
    using System.Numerics;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Objects;

    public class Rectangle : Mesh<OrthoVertex>
    {
        protected override void Initialize()
        {
            VertexBuffer = new();
            IndexBuffer = new();

            VertexBuffer.Append(new OrthoVertex(new Vector3(-1, 1, 0), new Vector2(0, 0)));
            VertexBuffer.Append(new OrthoVertex(new Vector3(-1, -1, 0), new Vector2(0, 1)));
            VertexBuffer.Append(new OrthoVertex(new Vector3(1, 1, 0), new Vector2(1, 0)));
            VertexBuffer.Append(new OrthoVertex(new Vector3(1, -1, 0), new Vector2(1, 1)));

            IndexBuffer.Append(0);
            IndexBuffer.Append(3);
            IndexBuffer.Append(1);
            IndexBuffer.Append(0);
            IndexBuffer.Append(2);
            IndexBuffer.Append(3);
        }
    }
}