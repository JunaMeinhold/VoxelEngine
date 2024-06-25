namespace VoxelEngine.Voxel
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

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
            Data = (int*)Marshal.AllocHGlobal(capacity * stride);
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
                    Data = (int*)Marshal.AllocHGlobal(value * stride);
                    capacity = value;
                    return;
                }

                Data = (int*)Marshal.ReAllocHGlobal((nint)Data, value * stride);
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

        public void Increase(int count)
        {
            EnsureCapacity(this.count + count);
            this.count += count;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((nint)Data);
            Data = null;
            capacity = 0;
            count = 0;
            GC.SuppressFinalize(this);
        }
    }
}