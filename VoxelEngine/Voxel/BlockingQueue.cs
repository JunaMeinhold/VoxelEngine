namespace VoxelEngine.Voxel
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    public class BlockingQueue<T>
    {
        private readonly Queue<T> queue = [];
        private readonly Lock _lock = new();
        private volatile int count;

        public int Count => count;

        public bool IsEmpty => count == 0;

        public void Lock()
        {
            _lock.Enter();
        }

        public void ReleaseLock()
        {
            _lock.Exit();
        }

        public bool Contains(T item)
        {
            Lock();
            var result = queue.Contains(item);
            ReleaseLock();
            return result;
        }

        public void EnqueueUnsafe(T item)
        {
            queue.Enqueue(item);
            count++;
        }

        public void EnqueueRange(IList<T> values)
        {
            Lock();
            int count = values.Count;
            queue.EnsureCapacity(queue.Count + count);
            foreach (T item in values)
            {
                queue.Enqueue(item);
            }
            this.count += count;
            ReleaseLock();
        }

        public void Enqueue(T item)
        {
            Lock();
            queue.Enqueue(item);
            count++;
            ReleaseLock();
        }

        public T Dequeue()
        {
            Lock();
            T item = queue.Dequeue();
            count--;
            ReleaseLock();
            return item;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            Lock();
            bool item = queue.TryDequeue(out result);
            if (item)
            {
                count--;
            }
            ReleaseLock();
            return item;
        }

        public bool TryDequeueUnsafe([MaybeNullWhen(false)] out T result)
        {
            bool item = queue.TryDequeue(out result);
            if (item)
            {
                count--;
            }

            return item;
        }
    }
}