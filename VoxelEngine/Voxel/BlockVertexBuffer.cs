namespace VoxelEngine.Voxel
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;

    public unsafe class BlockVertexBuffer : IDisposable, IVoxelVertexBuffer
    {
        private const int DefaultCapacity = 4096;
        private VertexBuffer<VoxelVertex>? vertexBuffer;
        private int count;
        private int capacity;
        private readonly uint stride = (uint)sizeof(VoxelVertex);

        private bool dirty = true;
        private bool bufferResized = false;

        private readonly Lock _lock = new();

        public VoxelVertex* Data;

        private string? debugName;
        private int vertexCount;

        public string? DebugName
        {
            get => debugName;
            set
            {
                debugName = value;
                if (vertexBuffer != null)
                {
                    //vertexBuffer.DebugName = value;
                }
            }
        }

        public bool Dirty => dirty;

        public BlockVertexBuffer()
        {
            capacity = DefaultCapacity;
            Data = AllocT<VoxelVertex>((uint)capacity * stride);
        }

        public int this[int index]
        {
            get => Data[index].Data;
            set => Data[index].Data = value;
        }

        public int Capacity
        {
            get => capacity;
            set
            {
                if (Data == null)
                {
                    Data = AllocT<VoxelVertex>((uint)value * stride);

                    capacity = value;
                    return;
                }

                Data = ReAllocT(Data, (uint)value * stride);
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

            dirty = true;
        }

        public void Append(int value)
        {
            lock (_lock)
            {
                EnsureCapacity(count + 1);
                Data[count].Data = value;
                count++;
            }
        }

        public void AppendRange(int* values, int count, Vector3 offset)
        {
            lock (_lock)
            {
                EnsureCapacity(this.count + count);
                for (int i = 0, j = this.count; i < count; i++, j++)
                {
                    Data[j] = new(values[i], offset);
                }
                this.count += count;
            }
        }

        public void Increase(int count)
        {
            EnsureCapacity(this.count + count);
            this.count += count;
        }

        public void BufferData(GraphicsContext context)
        {
            lock (_lock)
            {
                if (count > 0 && dirty)
                {
                    if (bufferResized || vertexBuffer == null)
                    {
                        if (vertexBuffer != null)
                        {
                            VertexBufferPool<VoxelVertex>.Shared.Return(vertexBuffer);
                            vertexBuffer = default;
                        }

                        vertexBuffer = VertexBufferPool<VoxelVertex>.Shared.Rent(capacity);

                        bufferResized = false;
                    }

                    vertexBuffer.Write(context, Data, count);
                    vertexCount = count;

                    dirty = false;
                }
            }
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

                Free(Data);
                Data = null;
                capacity = 0;
                count = 0;
                GC.SuppressFinalize(this);
            }
        }
    }
}