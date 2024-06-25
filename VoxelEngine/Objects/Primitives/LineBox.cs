namespace VoxelEngine.Objects.Primitives
{
    using Vortice.Direct3D;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Objects;

    public class LineBox : Mesh<LineVertex>
    {
        protected override void Initialize()
        {
            VertexBuffer = new();
            VertexBuffer.Topology = PrimitiveTopology.LineList;
            IndexBuffer = new();
            VertexBuffer.Append(new LineVertex(new(-0.5f, -0.5f, -0.5f, 0.5f)));//000
            VertexBuffer.Append(new LineVertex(new(-0.5f, -0.5f, 0.5f, 0.5f))); //001
            VertexBuffer.Append(new LineVertex(new(-0.5f, 0.5f, -0.5f, 0.5f))); //010
            VertexBuffer.Append(new LineVertex(new(-0.5f, 0.5f, 0.5f, 0.5f)));  //011
            VertexBuffer.Append(new LineVertex(new(0.5f, -0.5f, -0.5f, 0.5f))); //100
            VertexBuffer.Append(new LineVertex(new(0.5f, -0.5f, 0.5f, 0.5f)));  //101
            VertexBuffer.Append(new LineVertex(new(0.5f, 0.5f, -0.5f, 0.5f)));  //110
            VertexBuffer.Append(new LineVertex(new(0.5f, 0.5f, 0.5f, 0.5f)));   //111

            IndexBuffer.Append(7);//111
            IndexBuffer.Append(3);//011
            IndexBuffer.Append(5);//101
            IndexBuffer.Append(1);//001

            IndexBuffer.Append(6);//110
            IndexBuffer.Append(2);//010
            IndexBuffer.Append(4);//100
            IndexBuffer.Append(0);//000

            IndexBuffer.Append(5);//101
            IndexBuffer.Append(7);//111
            IndexBuffer.Append(4);//100
            IndexBuffer.Append(6);//110

            IndexBuffer.Append(1);//001
            IndexBuffer.Append(3);//011
            IndexBuffer.Append(0);//000
            IndexBuffer.Append(2);//010

            IndexBuffer.Append(7);//111
            IndexBuffer.Append(6);//110
            IndexBuffer.Append(3);//011
            IndexBuffer.Append(2);//010

            IndexBuffer.Append(5);//101
            IndexBuffer.Append(4);//100
            IndexBuffer.Append(1);//001
            IndexBuffer.Append(0);//000
        }
    }
}