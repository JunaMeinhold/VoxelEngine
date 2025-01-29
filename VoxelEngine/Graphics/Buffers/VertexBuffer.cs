namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using Hexa.NET.Utilities;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public unsafe class VertexBuffer<T> : Resource where T : unmanaged
    {
        private BufferDesc desc;
        private bool isDirty;
        private UnsafeList<T> vertices;
        private int vertexCount;
        private int capacity;
        private ComPtr<ID3D11Buffer> vertexBuffer;
        private readonly uint stride;
        private readonly CpuAccessFlag cpuAccessFlags;
        private readonly bool canWrite;
        private readonly bool canRead;

        public VertexBuffer(CpuAccessFlag cpuAccessFlags, int capacity)
        {
            var device = D3D11DeviceManager.Device;
            this.cpuAccessFlags = cpuAccessFlags;
            this.capacity = capacity;
            stride = (uint)sizeof(T);
            if (cpuAccessFlags == 0) throw new ArgumentException("Vertex Buffer cannot be immutable", nameof(cpuAccessFlags));
            isDirty = true;

            Usage usage = Usage.Immutable;
            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0)
            {
                usage = Usage.Dynamic;
                canWrite = true;
            }
            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0)
            {
                usage = Usage.Staging;
                canRead = true;
            }

            desc = new((uint)(capacity * sizeof(T)), usage, (uint)BindFlag.VertexBuffer, (uint)cpuAccessFlags);
            device.CreateBuffer(ref desc, null, out vertexBuffer);
        }

        public VertexBuffer(CpuAccessFlag cpuAccessFlags, Span<T> vertices)
        {
            var device = D3D11DeviceManager.Device;
            this.cpuAccessFlags = cpuAccessFlags;
            stride = (uint)sizeof(T);

            Usage usage = Usage.Immutable;
            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0)
            {
                usage = Usage.Dynamic;
                canWrite = true;
            }
            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0)
            {
                usage = Usage.Staging;
                canRead = true;
            }

            desc = new((uint)(vertices.Length * sizeof(T)), usage, (uint)BindFlag.VertexBuffer, (uint)cpuAccessFlags);
            fixed (T* pVertices = vertices)
            {
                SubresourceData subresource = new(pVertices);
                device.CreateBuffer(ref desc, ref subresource, out vertexBuffer);
            }
            vertexCount = vertices.Length;
        }

        public int Count => vertexCount;

        public int VertexCapacity => capacity;

        public unsafe int Stride { get; } = sizeof(T);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeBuffers()
        {
            var device = D3D11DeviceManager.Device;
            if (vertices.Count <= VertexCapacity)
            {
                return;
            }

            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
            }

            desc.ByteWidth = (uint)(vertices.Count * sizeof(T));
            SubresourceData data = new(vertices.Data);
            device.CreateBuffer(ref desc, ref data, out vertexBuffer);
            capacity = vertices.Count;
            //vertexBuffer.DebugName = debugName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void UpdateBuffers(ComPtr<ID3D11DeviceContext> context)
        {
            DeviceHelper.Write(context, vertexBuffer, vertices.ToArray());
            vertexCount = vertices.Count;
        }

        public void FreeMemory(ComPtr<ID3D11DeviceContext> context)
        {
            ResizeBuffers();
            UpdateBuffers(context);
            isDirty = false;
            vertices.Clear();
            vertices.Release();
        }

        public void Append(T vertex)
        {
            vertices.Add(vertex);
            isDirty = true;
        }

        public void Append(IEnumerable<T> vertices)
        {
            foreach (var vert in vertices)
            {
                this.vertices.PushBack(vert);
            }

            isDirty = true;
        }

        public void Append(T[] vertices)
        {
            this.vertices.AppendRange(vertices);
            isDirty = true;
        }

        public void Clear()
        {
            vertices.Clear();
            isDirty = true;
        }

        public void Bind(ComPtr<ID3D11DeviceContext> context)
        {
            Bind(context, 0);
        }

        public void Bind(ComPtr<ID3D11DeviceContext> context, int slot)
        {
            if (isDirty)
            {
                ResizeBuffers();
                UpdateBuffers(context);

                isDirty = false;
            }

            uint stride = this.stride;
            uint offset = 0;
            context.IASetVertexBuffers((uint)slot, 1, vertexBuffer.GetAddressOf(), &stride, &offset);
        }

        public static implicit operator ComPtr<ID3D11Buffer>(VertexBuffer<T> buffer) => buffer.vertexBuffer;

        protected override void DisposeCore()
        {
            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
                vertexBuffer = null;
            }
        }
    }
}