namespace VoxelEngine.Voxel
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;

    public unsafe class BlockVertexBuffer : IDisposable, IVoxelVertexBuffer
    {
        private const int DefaultCapacity = 4096;
        private ID3D11Buffer vertexBuffer;
        private int count;
        private int capacity;
        private readonly int stride;

        private bool Dirty = true;
        private bool bufferResized = true;

#if USE_LEGACY_LOADER
        public int* Data;
#else
        public VoxelVertex* Data;
#endif
        private string debugName;
        private int vertexCount;

        public string DebugName
        {
            get => debugName;
            set
            {
                debugName = value;
                if (vertexBuffer != null)
                {
                    vertexBuffer.DebugName = value;
                }
            }
        }

        public BlockVertexBuffer()
        {
            stride = sizeof(VoxelVertex);
            capacity = DefaultCapacity;
#if USE_LEGACY_LOADER
            Data = (int*)Marshal.AllocHGlobal(capacity * stride);
#else
            Data = (VoxelVertex*)Marshal.AllocHGlobal(capacity * stride);
#endif
        }

#if USE_LEGACY_LOADER
        public int this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }
#else

        public int this[int index]
        {
            get => Data[index].Data;
            set => Data[index].Data = value;
        }

#endif

        public int Capacity
        {
            get => capacity;
            set
            {
                if (Data == null)
                {
#if USE_LEGACY_LOADER
                    Data = (int*)Marshal.AllocHGlobal(value * stride);
#else
                    Data = (VoxelVertex*)Marshal.AllocHGlobal(value * stride);
#endif
                    capacity = value;
                    return;
                }
#if USE_LEGACY_LOADER
                Data = (int*)Marshal.ReAllocHGlobal((nint)Data, value * stride);
#else
                Data = (VoxelVertex*)Marshal.ReAllocHGlobal((nint)Data, value * stride);
#endif
                capacity = value;
                bufferResized = true;
                count = capacity < count ? capacity : count;
            }
        }

        public int Count => count;

        public int VertexCount => vertexCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int capacity)
        {
            int newcapacity = count == 0 ? DefaultCapacity : 2 * count;

            if (newcapacity < capacity)
            {
                newcapacity = capacity;
            }

            Capacity = newcapacity;
        }

        public void EnsureCapacity(int capacity)
        {
            if (this.capacity < capacity)
            {
                Grow(capacity);
            }
        }

        public void Reset(int length = DefaultCapacity)
        {
            EnsureCapacity(length);
            count = 0;

            Dirty = true;
        }

#if USE_LEGACY_LOADER
        public void Append(int value)
        {
            EnsureCapacity(count + 1);
            Data[count] = value;
            count++;
        }

        public void AppendRange(int* values, int count)
        {
            EnsureCapacity(this.count + count);
            MemcpyT(values, &Data[this.count], count);
            this.count += count;
        }
#else

        public void Append(int value)
        {
            EnsureCapacity(count + 1);
            Data[count] = new(value, default);
            count++;
        }

        public void AppendRange(int* values, int count, Vector3 offset)
        {
            EnsureCapacity(this.count + count);
            for (int i = 0, j = this.count; i < count; i++, j++)
            {
                Data[j] = new(values[i], offset);
            }
            this.count += count;
        }

#endif

        public void Increase(int count)
        {
            EnsureCapacity(this.count + count);
            this.count += count;
        }

        public void BufferData(ID3D11Device device)
        {
            if (count > 0 && Dirty)
            {
                if (bufferResized)
                {
                    vertexBuffer?.Dispose();
                    vertexBuffer = null;

                    vertexBuffer = device.CreateBuffer(new()
                    {
                        BindFlags = BindFlags.VertexBuffer,
                        CPUAccessFlags = CpuAccessFlags.Write,
                        MiscFlags = ResourceOptionFlags.None,
#if USE_LEGACY_LOADER
                    ByteWidth = Marshal.SizeOf<int>() * capacity,
#else
                        ByteWidth = Marshal.SizeOf<VoxelVertex>() * capacity,
#endif
                        Usage = ResourceUsage.Dynamic
                    }, new SubresourceData(Data));

                    vertexBuffer.DebugName = debugName;
                    bufferResized = false;
                }
                else
                {
                    DeviceHelper.Write(device.ImmediateContext, vertexBuffer, Data, count);
                }

                vertexCount = count;

                Dirty = false;
            }
        }

#if !USE_LEGACY_LOADER

        public void BufferData(ChunkVertexBuffer vertexBuffer, Vector3 offset)
        {
            if (vertexBuffer == null || vertexBuffer.Count == 0)
            {
                return;
            }
            if (vertexBuffer.Count % 3 != 0)
                throw new();
            AppendRange(vertexBuffer.Data, vertexBuffer.Count, offset);
            Dirty = true;
        }

#endif

        public bool Bind(ID3D11DeviceContext context)
        {
            if (vertexBuffer is null)
            {
                return false;
            }

            context.IASetVertexBuffer(0, vertexBuffer, stride);
            context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            return true;
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            vertexBuffer = null;
            Marshal.FreeHGlobal((nint)Data);
            Data = null;
            capacity = 0;
            count = 0;
            GC.SuppressFinalize(this);
        }
    }
}