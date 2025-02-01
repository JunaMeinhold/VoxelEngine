namespace VoxelEngine.Voxel.Meshing
{
    using Hexa.NET.D3D11;
    using System;
    using System.Numerics;
    using System.Threading;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;

    public unsafe class RegionVertexBuffer : IDisposable
    {
        private VertexBuffer<VoxelVertex>? vertexBuffer;
        private int count;
        private readonly uint stride = (uint)sizeof(VoxelVertex);

        private bool dirty = false;
        private bool bufferResized = false;

        private readonly Lock _lock = new();

        private int vertexCount;
        private MappedSubresource mappedResource;

        public RegionVertexBuffer()
        {
        }

        public int Count => count;

        public int VertexCount => vertexCount;

        public void Map(GraphicsContext context, int size)
        {
            _lock.Enter();

            count = 0;

            dirty = true;

            if (bufferResized || vertexBuffer == null)
            {
                if (vertexBuffer != null)
                {
                    VertexBufferPool<VoxelVertex>.Shared.Return(vertexBuffer);
                    vertexBuffer = default;
                }

                vertexBuffer = VertexBufferPool<VoxelVertex>.Shared.Rent(size);

                bufferResized = false;
            }

            mappedResource = context.Map(vertexBuffer, 0, Hexa.NET.D3D11.Map.WriteDiscard, 0);
        }

        public void Unmap(GraphicsContext context)
        {
            mappedResource = default;
            context.Unmap(vertexBuffer!, 0);
            vertexCount = count;
            dirty = false;
            _lock.Exit();
        }

        private void AppendRange(VoxelVertex* values, int count, Vector3 offset)
        {
            int newCount = this.count + count;
            if (newCount > vertexBuffer!.Count) return;
            VoxelVertex* vertices = (VoxelVertex*)mappedResource.PData;
            for (int i = 0, j = this.count; i < count; i++, j++)
            {
                var v = values[i];
                v.Offset = offset;
                vertices[j] = v;
            }
            this.count = newCount;
        }

        public void BufferData(ChunkVertexBuffer vertexBuffer, Vector3 offset)
        {
            if (vertexBuffer == null || vertexBuffer.Count == 0)
            {
                return;
            }
            if (vertexBuffer.Count % 3 != 0)
                throw new();
            AppendRange(vertexBuffer.Data, vertexBuffer.Count, offset);
            dirty = true;
        }

        public bool Bind(GraphicsContext context)
        {
            if (vertexBuffer == null)
            {
                return false;
            }

            context.SetVertexBuffer(vertexBuffer, stride);
            return true;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (vertexBuffer != null)
                {
                    VertexBufferPool<VoxelVertex>.Shared.Return(vertexBuffer);
                    vertexBuffer = default;
                }

                count = 0;
                GC.SuppressFinalize(this);
            }
        }

        public void Lock()
        {
            throw new NotImplementedException();
        }

        public void ReleaseLock()
        {
            throw new NotImplementedException();
        }
    }
}