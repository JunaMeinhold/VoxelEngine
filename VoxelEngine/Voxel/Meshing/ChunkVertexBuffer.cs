namespace VoxelEngine.Voxel.Meshing
{
    using System.Runtime.CompilerServices;
    using System.Threading;

    public unsafe class ChunkVertexBuffer : IDisposable, IVoxelVertexBuffer
    {
        private const int DefaultCapacity = 4096;
        private readonly int stride;
        private int count;
        private int capacity;

        public VoxelVertex* Data;

        private readonly SemaphoreSlim _lock = new(1);

        public ChunkVertexBuffer()
        {
            stride = sizeof(int);
            capacity = DefaultCapacity;
            Data = AllocT<VoxelVertex>(capacity);
            ZeroMemoryT(Data, capacity);
        }

        public VoxelVertex this[int index]
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
                    Data = AllocT<VoxelVertex>(value);
                    capacity = value;
                    return;
                }

                Data = ReAllocT(Data, value);
                capacity = value;
                count = capacity < count ? capacity : count;
                ZeroMemoryT(Data + count, capacity - count);
            }
        }

        public int Count => count;

        public void Lock()
        {
            _lock.Wait();
        }

        public void ReleaseLock()
        {
            _lock.Release();
        }

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

        public VoxelVertex* Increase(int count)
        {
            if (count <= 0) return null;

            int oldCount = this.count;
            int newSize = oldCount + count;

            if (newSize > capacity)
            {
                int newCapacity = Math.Max(capacity * 2, newSize);
                VoxelVertex* tmp = AllocT<VoxelVertex>(newCapacity);
                MemcpyT(Data, tmp, oldCount);
                Free(Data);
                Data = tmp;
                capacity = newCapacity;
            }

            VoxelVertex* ptr = Data + oldCount;
            this.count = newSize;

            return ptr;
        }

        public void Dispose()
        {
            Lock();
            if (Data != null)
            {
                Free(Data);
                Data = null;
                capacity = 0;
                count = 0;
            }
            GC.SuppressFinalize(this);
            ReleaseLock();
        }
    }
}