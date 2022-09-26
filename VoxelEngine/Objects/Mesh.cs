namespace VoxelEngine.Objects
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Resources;

    public abstract class Mesh<T> : Resource where T : unmanaged
    {
        public Mesh()
        {
            Initialize();
        }

        public VertexBuffer<T> VertexBuffer;
        public IndexBuffer IndexBuffer;

        public bool HasVertexBuffer => VertexBuffer != null;

        public bool HasIndexBuffer => IndexBuffer != null;

        protected abstract void Initialize();

        protected virtual void Uninitialize()
        {
            VertexBuffer?.Dispose();
            VertexBuffer = null;
            IndexBuffer?.Dispose();
            IndexBuffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BindType Bind(ID3D11DeviceContext context)
        {
            if (HasIndexBuffer)
            {
                VertexBuffer.Bind(context);
                IndexBuffer.Bind(context);
                return BindType.Indexed;
            }
            if (HasVertexBuffer)
            {
                VertexBuffer.Bind(context);
                return BindType.Vertex;
            }
            return BindType.None;
        }

        public BindType Bind(ID3D11DeviceContext context, int slot)
        {
            if (HasIndexBuffer)
            {
                VertexBuffer.Bind(context, slot);
                IndexBuffer.Bind(context);
                return BindType.Indexed;
            }
            if (HasVertexBuffer)
            {
                VertexBuffer.Bind(context, slot);
                return BindType.Vertex;
            }
            return BindType.None;
        }

        public void DrawAuto(ID3D11DeviceContext context, Pipeline pipeline)
        {
            if (HasIndexBuffer)
            {
                VertexBuffer.Bind(context, 0);
                IndexBuffer.Bind(context);
                pipeline.DrawIndexed(context, IndexBuffer.IndexCount, 0, 0);
                return;
            }
            if (HasVertexBuffer)
            {
                VertexBuffer.Bind(context, 0);
                pipeline.Draw(context, VertexBuffer.VertexCount, 0);
                return;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Uninitialize();
        }
    }
}