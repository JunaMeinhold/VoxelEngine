namespace VoxelEngine.Rendering.D3D
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Resources;

    public class VertexBuffer<T> : Resource where T : unmanaged
    {
        private bool isDirty;
        private List<T> vertices;
        private int vertexCount;
        private ID3D11Buffer vertexBuffer;
        private string debugName;
        private readonly int size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexBuffer()
        {
            size = Marshal.SizeOf<T>();
            vertices = new List<T>();
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexBuffer(ID3D11Device device, IEnumerable<T> vertices)
        {
            size = Marshal.SizeOf<T>();
            this.vertices = new List<T>(vertices);
            ResizeBuffers(device);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VertexBuffer(ID3D11Device device, int capacity)
        {
            size = Marshal.SizeOf<T>();
            vertices = new List<T>(capacity);
            ResizeBuffers(device);
        }

        public int VertexCount => vertexCount;

        public int VertexCapacity { get; private set; }

        public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

        public InstanceBuffer InstanceBuffer;

        public string DebugName
        {
            get => debugName; set
            {
                debugName = value;
                if (vertexBuffer != null)
                {
                    vertexBuffer.DebugName = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeBuffers(ID3D11Device device)
        {
            if (vertices.Count <= VertexCapacity)
            {
                return;
            }

            vertexBuffer?.Dispose();
            vertexBuffer = device.CreateBuffer((ReadOnlySpan<T>)vertices.ToArray().AsSpan(),
                new BufferDescription(
                vertices.Count * sizeof(T),
                BindFlags.VertexBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write));
            VertexCapacity = vertices.Count;
            vertexBuffer.DebugName = debugName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void UpdateBuffers(ID3D11DeviceContext context)
        {
            DeviceHelper.Write(context, vertexBuffer, vertices.ToArray());
            vertexCount = vertices.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FreeMemory(ID3D11DeviceContext context)
        {
            ResizeBuffers(context.Device);
            UpdateBuffers(context);
            isDirty = false;
            vertices.Clear();
            vertices = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T vertex)
        {
            vertices.Add(vertex);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(params T[] vertices)
        {
            this.vertices.AddRange(vertices);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            vertices.Clear();
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context)
        {
            Bind(context, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context, int slot)
        {
            if (isDirty)
            {
                ResizeBuffers(context.Device);
                UpdateBuffers(context);

                isDirty = false;
            }

            if (InstanceBuffer != null)
            {
                InstanceBuffer.Bind(context, vertexBuffer, size);
            }
            else
            {
                context.IASetVertexBuffer(slot, vertexBuffer, size);
            }

            context.IASetPrimitiveTopology(Topology);
        }

        protected override void Dispose(bool disposing)
        {
            vertexBuffer?.Dispose();
            vertexBuffer = null;
        }
    }
}