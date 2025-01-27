namespace VoxelEngine.Core.Unsafes
{
    using System.Numerics;

    public unsafe struct UnsafeRingBuffer<T> where T : unmanaged, INumber<T>
    {
        private T* rawValues;
        private T* avgValues;
        private int length;
        private int head = 0;
        private int tail;
        private T sum;
        private T countT;
        private int count = 0;
        private bool averageValues = true;

        public UnsafeRingBuffer(int length)
        {
            rawValues = AllocT<T>(length);
            avgValues = AllocT<T>(length);
            this.length = length;
        }

        public readonly T* Raw => rawValues;

        public readonly T* Values
        {
            get
            {
                return averageValues ? avgValues : rawValues;
            }
        }

        public readonly int Length => length;

        public readonly int Tail => tail;

        public readonly int Head => head;

        public readonly T TailValue => Values[tail];

        public readonly T HeadValue => Values[head - 1];

        public bool AverageValues { readonly get => averageValues; set => averageValues = value; }

        public void Add(T value)
        {
            if (value < default(T))
            {
                value = default;
            }

            // Subtract the oldest value from the sum if the buffer is full
            if (count == length)
            {
                sum -= rawValues[tail];
            }
            else
            {
                count++;
                countT++;
            }

            // Add the new value to the sum
            sum += value;

            avgValues[head] = CalculateAverage();
            rawValues[head] = value;

            head = (head + 1) % length;
            tail = (head - count + length) % length;
        }

        public readonly T CalculateAverage()
        {
            if (count == 0)
            {
                // The buffer is empty, return the default value of T
                return default;
            }

            // Calculate and return the average
            return sum / countT;
        }

        public void Release()
        {
            Free(rawValues);
            Free(avgValues);
            this = default;
        }
    }
}