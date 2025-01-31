namespace VoxelEngine.Voxel.Metadata
{
    using Hexa.NET.Mathematics;
    using System.Numerics;

    public class BiomeMetadata
    {
        public byte[] Data = new byte[Chunk.CHUNK_SIZE_SQUARED];

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
            BiomeMetadataHeader.Write(stream, Data.Length);
            stream.Write(Data);
        }

        public void Deserialize(Stream stream)
        {
            BiomeMetadataHeader.Read(stream, out int dataLength);
            Data = new byte[dataLength];
            stream.ReadExactly(Data);
        }

        public int Deserialize(ReadOnlySpan<byte> data)
        {
            int index = BiomeMetadataHeader.Read(data, out int dataLength);
            Data = new byte[dataLength];
            data.Slice(index, dataLength).CopyTo(Data);
            return index + dataLength;
        }
    }
}