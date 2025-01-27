namespace VoxelEngine.Voxel.Metadata
{
    using System.Buffers.Binary;
    using System.Collections;
    using VoxelEngine.IO;

    public class BlockMetadataCollection : IList<BlockMetadata>, ICollection<BlockMetadata>
    {
        private readonly List<BlockMetadata> metadata = new();

        public BlockMetadata this[int index] { get => metadata[index]; set => metadata[index] = value; }

        public int Count => metadata.Count;

        public bool IsReadOnly => false;

        public void Add(BlockMetadata item)
        {
            metadata.Add(item);
        }

        public void Clear()
        {
            metadata.Clear();
        }

        public bool Contains(BlockMetadata item)
        {
            return metadata.Contains(item);
        }

        public void CopyTo(BlockMetadata[] array, int arrayIndex)
        {
            metadata.CopyTo(array, arrayIndex);
        }

        public IEnumerator<BlockMetadata> GetEnumerator()
        {
            return metadata.GetEnumerator();
        }

        public int IndexOf(BlockMetadata item)
        {
            return metadata.IndexOf(item);
        }

        public void Insert(int index, BlockMetadata item)
        {
            metadata.Insert(index, item);
        }

        public bool Remove(BlockMetadata item)
        {
            return metadata.Remove(item);
        }

        public void RemoveAt(int index)
        {
            metadata.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return metadata.GetEnumerator();
        }

        public void Serialize(Stream stream)
        {
            const int defaultBufferSize = 4096;
            BlockMetadataCollectionHeader.Write(stream, metadata.Count);
            byte[] buffer = new byte[defaultBufferSize];
            for (int i = 0; i < metadata.Count; i++)
            {
                BlockMetadata data = metadata[i];
                int size = data.SizeOf();
                stream.WriteInt32(size);
                if (size > buffer.Length)
                {
                    buffer = new byte[(int)(size * 1.5f)];
                }
                data.Write(buffer);
                stream.Write(buffer, 0, size);
            }
        }

        public void Deserialize(Stream stream)
        {
            const int defaultBufferSize = 4096;
            BlockMetadataCollectionHeader.Read(stream, out int metadataCount);
            metadata.Capacity = metadataCount;
            byte[] buffer = new byte[defaultBufferSize];
            for (int i = 0; i < metadataCount; i++)
            {
                int size = stream.ReadInt32();
                if (size > buffer.Length)
                {
                    buffer = new byte[(int)(size * 1.5f)];
                }
                stream.ReadExactly(buffer, 0, size);
                metadata.Add(BlockMetadata.ReadFrom(buffer));
            }
        }

        public int Deserialize(ReadOnlySpan<byte> data)
        {
            int index = BlockMetadataCollectionHeader.Read(data, out int metadataCount);
            metadata.Capacity = metadataCount;
            for (int i = 0; i < metadataCount; i++)
            {
                int size = BinaryPrimitives.ReadInt32LittleEndian(data[index..]);
                index += 4;

                metadata.Add(BlockMetadata.ReadFrom(data.Slice(index, size)));
                index += size;
            }
            return index;
        }
    }
}