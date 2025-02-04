namespace VoxelEngine.Voxel.Meshing
{
    using Hexa.NET.Utilities;
    using System.Runtime.CompilerServices;

    public struct SemaphoreLight
    {
        private volatile int count;
        private readonly int maxCount;

        public SemaphoreLight(int initialCount, int maxCount)
        {
            if (initialCount < 0 || maxCount <= 0 || initialCount > maxCount)
            {
                throw new ArgumentException("Invalid semaphore initialization values.");
            }

            count = initialCount;
            this.maxCount = maxCount;
        }

        public readonly int CurrentCount => count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            while (true)
            {
                int oldCount = count;
                if (count > 0 && Interlocked.CompareExchange(ref count, oldCount - 1, oldCount) == oldCount)
                {
                    return;
                }

                Thread.Yield();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (count >= maxCount)
            {
                throw new SemaphoreFullException("Semaphore already at maximum count.");
            }

            Interlocked.Increment(ref count);
        }
    }

    public unsafe struct ChunkVertexBuffer2
    {
        private const int DefaultCapacity = 4096;
        private readonly int stride;
        private int count;
        private int capacity;
        private SemaphoreLight semaphore = new(1, 1);

        public VoxelVertex* Data;

        public ChunkVertexBuffer2()
        {
            stride = sizeof(int);
            capacity = DefaultCapacity;
            Data = AllocT<VoxelVertex>(capacity);
        }

        public VoxelVertex this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public int Capacity
        {
            readonly get => capacity;
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
            }
        }

        public readonly int Count => count;

        public void Lock()
        {
            semaphore.Wait();
        }

        public void ReleaseLock()
        {
            semaphore.Release();
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
            ReleaseLock();
        }
    }
}