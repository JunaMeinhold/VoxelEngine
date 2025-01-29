namespace VoxelEngine.Voxel
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.D3D11;

    public unsafe class BlockVertexBuffer : IDisposable, IVoxelVertexBuffer
    {
        private const int DefaultCapacity = 4096;
        private ComPtr<ID3D11Buffer> vertexBuffer;
        private int count;
        private int capacity;
        private readonly uint stride;

        private bool Dirty = true;
        private bool bufferResized = true;

#if USE_LEGACY_LOADER
        public int* Data;
#else
        public VoxelVertex* Data;
#endif
        private string? debugName;
        private int vertexCount;

        public string? DebugName
        {
            get => debugName;
            set
            {
                debugName = value;
                if (vertexBuffer.Handle != null)
                {
                    //vertexBuffer.DebugName = value;
                }
            }
        }

        public BlockVertexBuffer()
        {
            stride = (uint)sizeof(VoxelVertex);
            capacity = DefaultCapacity;
#if USE_LEGACY_LOADER
            Data = AllocT<int>((uint)capacity * stride);
#else
            Data = AllocT<VoxelVertex>((uint)capacity * stride);
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
                    Data = AllocT<int>((uint)value * stride);
#else
                    Data = AllocT<VoxelVertex>((uint)value * stride);
#endif
                    capacity = value;
                    return;
                }
#if USE_LEGACY_LOADER
                Data = ReAllocT(Data, (uint)value * stride);
#else
                Data = ReAllocT(Data, (uint)value * stride);
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

        public void BufferData(ComPtr<ID3D11DeviceContext> context)
        {
            if (count > 0 && Dirty)
            {
                if (bufferResized)
                {
                    var device = D3D11DeviceManager.Device;
                    if (vertexBuffer.Handle != null)
                    {
                        vertexBuffer.Release();
                        vertexBuffer = default;
                    }

                    BufferDesc desc = new()
                    {
                        BindFlags = (uint)BindFlag.VertexBuffer,
                        CPUAccessFlags = (uint)CpuAccessFlag.Write,
                        MiscFlags = 0,
#if USE_LEGACY_LOADER
                        ByteWidth = (uint)(sizeof(int) * capacity),
#else
                        ByteWidth = (uint)(sizeof(VoxelVertex) * capacity),
#endif
                        Usage = Usage.Dynamic
                    };

                    SubresourceData subresourceData = new(Data);
                    device.CreateBuffer(&desc, &subresourceData, out vertexBuffer);

                    //vertexBuffer.DebugName = debugName;
                    bufferResized = false;
                }
                else
                {
                    DeviceHelper.Write(context, vertexBuffer, Data, count);
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

        public bool Bind(ComPtr<ID3D11DeviceContext> context)
        {
            if (vertexBuffer.Handle == null)
            {
                return false;
            }

            var vtxBuffer = vertexBuffer.Handle;
            uint stride = this.stride;
            uint offset = 0;
            context.IASetVertexBuffers(0, 1, &vtxBuffer, &stride, &offset);
            context.IASetPrimitiveTopology(PrimitiveTopology.Trianglelist);
            return true;
        }

        public void Dispose()
        {
            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
                vertexBuffer = default;
            }

            Free(Data);
            Data = null;
            capacity = 0;
            count = 0;
            GC.SuppressFinalize(this);
        }
    }
}