namespace VoxelEngine.Voxel
{
    public unsafe struct BlockStorage
    {
        private readonly int size;
        public Block* Data;

        public BlockStorage(int size)
        {
            this.size = size;
            Allocate();
        }

        public void Allocate()
        {
            Data = AllocT<Block>(size);
            ZeroMemoryT(Data, size);
        }

        public readonly bool IsAllocated => Data != null;

        public Block this[int index]
        {
            get
            {
                if (Data == null)
                {
                    return new Block(0);
                }
                return Data[index];
            }
            set
            {
                if (Data == null)
                {
                    Allocate();
                }

                Data[index] = value;
            }
        }

        public void Dispose()
        {
            if (Data != null)
            {
                Free(Data);
                Data = null;
            }
        }
    }
}