namespace VoxelEngine.Graphics.Primitives
{
    using System.Numerics;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.Shaders;

    public class Plane : IPrimitive
    {
        private readonly VertexBuffer<MeshVertex> vertexBuffer;
        private readonly IndexBuffer indexBuffer;
        private bool disposedValue;

        public Plane(ID3D11Device device, float size)
        {
            CreatePlane(device, out vertexBuffer, out indexBuffer, size);
        }

        public static void CreatePlane(ID3D11Device device, out VertexBuffer<MeshVertex> vertexBuffer, out IndexBuffer indexBuffer, float size = 1)
        {
            vertexBuffer = new(device, new MeshVertex[]
            {
             new(new(-1 * size, 1 * size, 0), new Vector2(0,0), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(-1 * size, -1 * size, 0), new Vector2(0,1), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(1 * size, 1 * size, 0), new Vector2(1,0), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(1 * size, -1 * size, 0), new Vector2(1,1), new(0,0,-1), new(1,0,0), new(0,1,0))
            });

            indexBuffer = new(device, new int[]
            {
             0, 3, 1,
             0, 2, 3
            });
        }

        public void DrawAuto(ID3D11DeviceContext context, GraphicsPipeline pipeline)
        {
            pipeline.Begin(context);
            context.IASetVertexBuffer(0, vertexBuffer, vertexBuffer.Stride);
            if (indexBuffer != null)
            {
                indexBuffer.Bind(context);
                context.DrawIndexedInstanced(indexBuffer.Count, 1, 0, 0, 0);
                context.IASetIndexBuffer(null, 0, 0);
            }
            else
            {
                context.DrawInstanced(vertexBuffer.Count, 1, 0, 0);
            }
            context.IASetVertexBuffer(0, null, 0);
            pipeline.End(context);
        }

        public void DrawAuto(ID3D11DeviceContext context)
        {
            context.IASetVertexBuffer(0, vertexBuffer, vertexBuffer.Stride);
            if (indexBuffer != null)
            {
                indexBuffer.Bind(context);
                context.DrawIndexedInstanced(indexBuffer.Count, 1, 0, 0, 0);
                context.IASetIndexBuffer(null, 0, 0);
            }
            else
            {
                context.DrawInstanced(vertexBuffer.Count, 1, 0, 0);
            }
            context.IASetVertexBuffer(0, null, 0);
        }

        public void Bind(ID3D11DeviceContext context, out int vertexCount, out int indexCount, out int instanceCount)
        {
            context.IASetVertexBuffer(0, vertexBuffer, vertexBuffer.Stride);
            vertexCount = vertexBuffer.Count;
            indexBuffer.Bind(context);
            indexCount = indexBuffer?.Count ?? 0;
            instanceCount = 1;
        }

        public void Unbind(ID3D11DeviceContext context)
        {
            context.IASetVertexBuffer(0, null, 0);
            context.IASetIndexBuffer(null, 0, 0);
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