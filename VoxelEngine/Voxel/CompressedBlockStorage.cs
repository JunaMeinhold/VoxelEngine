namespace VoxelEngine.Voxel
{
    using Hexa.NET.Utilities;

    public unsafe struct CompressedBlockStorage
    {
        private readonly int blockCount;
        private int maxBlockId;
        private int bitsPerBlock;

        private readonly UnsafeDictionary<ushort, ushort> blockPalette = new() { { 0, 0 } };
        private readonly UnsafeList<ushort> reversePalette = [0];

        private byte* data;

        public CompressedBlockStorage(int maxBlockId, int blockCount)
        {
            this.maxBlockId = maxBlockId;
            this.blockCount = blockCount;

            if (maxBlockId == 0)
            {
                bitsPerBlock = 0;
                return;
            }

            bitsPerBlock = (int)Math.Ceiling(Math.Log2(maxBlockId + 1));
            int totalBits = blockCount * bitsPerBlock;
            int totalBytes = (totalBits + 7) / 8;
            data = AllocT<byte>(totalBytes);
            for (int i = 0; i < totalBytes; i++)
            {
                data[i] = 0;
            }
        }

        public readonly bool IsAllocated => data != null;

        public void Dispose()
        {
            if (data != null)
            {
                Free(data);
                data = null;
            }
            blockPalette.Release();
            reversePalette.Release();
            maxBlockId = 0;
        }

        public Block this[int index]
        {
            get
            {
                if (data == null)
                {
                    return new Block(0);
                }

                var bitOffset = bitsPerBlock * index;
                var byteOffset = bitOffset >> 3;
                var bitInByteOffset = bitOffset & 7;

                uint value = 0;
                int remainingBits = bitsPerBlock;
                int shift = 0;

                while (remainingBits > 0)
                {
                    int bitsInCurrentByte = Math.Min(remainingBits, 8 - bitInByteOffset);
                    value |= (uint)((data[byteOffset] >> bitInByteOffset) & ((1 << bitsInCurrentByte) - 1)) << shift;
                    remainingBits -= bitsInCurrentByte;
                    shift += bitsInCurrentByte;
                    bitInByteOffset = 0;
                    byteOffset++;
                }

                ushort blockId = reversePalette[(int)value];
                return new Block(blockId);
            }
            set
            {
                var blockId = value.Type;

                if (!blockPalette.TryGetValue(blockId, out ushort valueToSet))
                {
                    valueToSet = (ushort)reversePalette.Count;
                    blockPalette.Add(blockId, valueToSet);
                    reversePalette.Add(blockId);

                    if (data == null || valueToSet > maxBlockId)
                    {
                        ResizeStorage(valueToSet);
                    }
                }

                var bitOffset = bitsPerBlock * index;
                var byteOffset = bitOffset >> 3;
                var bitInByteOffset = bitOffset & 7;

                int remainingBits = bitsPerBlock;

                while (remainingBits > 0)
                {
                    int bitsInCurrentByte = Math.Min(remainingBits, 8 - bitInByteOffset);
                    uint mask = (uint)((1 << bitsInCurrentByte) - 1);
                    data[byteOffset] = (byte)((data[byteOffset] & ~(mask << bitInByteOffset)) | ((valueToSet & mask) << bitInByteOffset));
                    valueToSet >>= bitsInCurrentByte;
                    remainingBits -= bitsInCurrentByte;
                    bitInByteOffset = 0;
                    byteOffset++;
                }
            }
        }

        private void ResizeStorage(int newMaxBlockId)
        {
            int newBitsPerBlock = (int)Math.Ceiling(Math.Log2(newMaxBlockId + 1));
            if (newBitsPerBlock <= bitsPerBlock)
            {
                return;
            }

            int newTotalBits = blockCount * newBitsPerBlock;
            int newTotalBytes = (newTotalBits + 7) / 8;
            byte* newData = AllocT<byte>(newTotalBytes);

            for (int i = 0; i < newTotalBytes; i++)
            {
                newData[i] = 0;
            }

            for (int i = 0; i < blockCount; i++)
            {
                var bitOffset = bitsPerBlock * i;
                var byteOffset = bitOffset >> 3;
                var bitInByteOffset = bitOffset & 7;

                uint value = 0;
                int remainingBits = bitsPerBlock;
                int shift = 0;

                while (remainingBits > 0)
                {
                    int bitsInCurrentByte = Math.Min(remainingBits, 8 - bitInByteOffset);
                    value |= (uint)((data[byteOffset] >> bitInByteOffset) & ((1 << bitsInCurrentByte) - 1)) << shift;
                    remainingBits -= bitsInCurrentByte;
                    shift += bitsInCurrentByte;
                    bitInByteOffset = 0;
                    byteOffset++;
                }

                bitOffset = newBitsPerBlock * i;
                byteOffset = bitOffset >> 3;
                bitInByteOffset = bitOffset & 7;

                remainingBits = newBitsPerBlock;
                uint valueToSet = value;

                while (remainingBits > 0)
                {
                    int bitsInCurrentByte = Math.Min(remainingBits, 8 - bitInByteOffset);
                    uint mask = (uint)((1 << bitsInCurrentByte) - 1);
                    newData[byteOffset] = (byte)((newData[byteOffset] & ~(mask << bitInByteOffset)) | ((valueToSet & mask) << bitInByteOffset));
                    valueToSet >>= bitsInCurrentByte;
                    remainingBits -= bitsInCurrentByte;
                    bitInByteOffset = 0;
                    byteOffset++;
                }
            }

            Free(data);
            data = newData;
            bitsPerBlock = newBitsPerBlock;
            maxBlockId = newMaxBlockId;
        }
    }
}