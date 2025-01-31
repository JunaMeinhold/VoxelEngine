namespace VoxelEngine.Voxel
{
    using System.Runtime.CompilerServices;

    public unsafe class ChunkVertexBuffer : IDisposable, IVoxelVertexBuffer
    {
        private const int DefaultCapacity = 4096;
        private readonly int stride;
        private int count;
        private int capacity;

        public int* Data;

        public ChunkVertexBuffer()
        {
            stride = sizeof(int);
            capacity = DefaultCapacity;
            Data = AllocT<int>(capacity);
        }

        public int this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public int Capacity
        {
            get => capacity;
            set
            {
                if (Data == null)
                {
                    Data = AllocT(value);
                    capacity = value;
                    return;
                }

                Data = ReAllocT(Data, value);
                capacity = value;
                count = capacity < count ? capacity : count;
            }
        }

        public int Count => count;

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

        public void Reset(int length)
        {
            EnsureCapacity(length);
            count = 0;
        }

        public void Append(int value)
        {
            EnsureCapacity(count + 1);
            Data[count] = value;
            count++;
        }

        public void AppendRange(int* values, int count)
        {
            int newSize = this.count + count;
            EnsureCapacity(newSize);
            MemcpyT(values, Data + this.count, count);
            this.count = newSize;
        }

        public void Increase(int count)
        {
            EnsureCapacity(this.count + count);
            this.count += count;
        }

        public void Dispose()
        {
            Free(Data);
            Data = null;
            capacity = 0;
            count = 0;
            GC.SuppressFinalize(this);
        }
    }
}