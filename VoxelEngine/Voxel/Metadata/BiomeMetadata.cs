namespace VoxelEngine.Voxel.Metadata
{
    using Hexa.NET.Mathematics;

    public unsafe struct BiomeMetadata
    {
        public fixed byte Data[Chunk.CHUNK_SIZE_SQUARED];

        public BiomeMetadata()
        {
        }

        public byte this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public byte this[Point2 position]
        {
            get => Data[position.MapToIndex()];
            set => Data[position.MapToIndex()] = value;
        }

        public void Serialize(Stream stream)
        {
            fixed (byte* pData = Data)
            {
                BiomeMetadataHeader.Write(stream, Chunk.CHUNK_SIZE_SQUARED);
                stream.Write(new Span<byte>(pData, Chunk.CHUNK_SIZE_SQUARED));
            }
        }

        public void Deserialize(Stream stream)
        {
            fixed (byte* pData = Data)
            {
                BiomeMetadataHeader.Read(stream, out int dataLength);
                stream.ReadExactly(new Span<byte>(pData, dataLength));
            }
        }
    }
}