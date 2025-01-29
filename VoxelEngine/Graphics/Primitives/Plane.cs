namespace VoxelEngine.Graphics.Primitives
{
    using System.Numerics;
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Mathematics;

    public unsafe class Plane : IPrimitive
    {
        private readonly VertexBuffer<MeshVertex> vertexBuffer;
        private readonly IndexBuffer<ushort> indexBuffer;
        private bool disposedValue;

        public Plane(float size)
        {
            CreatePlane(out vertexBuffer, out indexBuffer, size);
        }

        public static void CreatePlane(out VertexBuffer<MeshVertex> vertexBuffer, out IndexBuffer<ushort> indexBuffer, float size = 1)
        {
            vertexBuffer = new(0,
            [
             new(new(-1 * size, 1 * size, 0), new Vector2(0,0), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(-1 * size, -1 * size, 0), new Vector2(0,1), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(1 * size, 1 * size, 0), new Vector2(1,0), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(1 * size, -1 * size, 0), new Vector2(1,1), new(0,0,-1), new(1,0,0), new(0,1,0))
            ]);

            indexBuffer = new(0,
            [
             0, 3, 1,
             0, 2, 3
            ]);
        }

        public void DrawAuto(ComPtr<ID3D11DeviceContext> context, GraphicsPipelineState pso)
        {
            pso.Begin(context);
            vertexBuffer.Bind(context, 0);
            if (indexBuffer != null)
            {
                indexBuffer.Bind(context);
                context.DrawIndexedInstanced((uint)indexBuffer.Count, 1, 0, 0, 0);
                context.IAUnsetIndexBuffer();
            }
            else
            {
                context.DrawInstanced((uint)vertexBuffer.Count, 1, 0, 0);
            }
            context.IAUnsetVertexBuffers(0, 1);
            pso.End(context);
        }

        public void DrawAuto(ComPtr<ID3D11DeviceContext> context)
        {
            vertexBuffer.Bind(context, 0);
            if (indexBuffer != null)
            {
                indexBuffer.Bind(context);
                context.DrawIndexedInstanced((uint)indexBuffer.Count, 1, 0, 0, 0);
                context.IAUnsetIndexBuffer();
            }
            else
            {
                context.DrawInstanced((uint)vertexBuffer.Count, 1, 0, 0);
            }
            context.IAUnsetVertexBuffers(0, 1);
        }

        public void Bind(ComPtr<ID3D11DeviceContext> context, out int vertexCount, out int indexCount, out int instanceCount)
        {
            vertexBuffer.Bind(context, 0);
            vertexCount = vertexBuffer.Count;
            indexBuffer.Bind(context);
            indexCount = indexBuffer?.Count ?? 0;
            instanceCount = 1;
        }

        public void Unbind(ComPtr<ID3D11DeviceContext> context)
        {
            context.IAUnsetVertexBuffers(0, 1);
            context.IAUnsetIndexBuffer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
                disposedValue = true;
            }
        }

        ~Plane()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}