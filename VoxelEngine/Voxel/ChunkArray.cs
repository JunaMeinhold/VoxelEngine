namespace VoxelEngine.Voxel
{
    using System.Collections.Concurrent;
    using System.Numerics;

    public class ChunkArray
    {
        private readonly ConcurrentDictionary<Vector3, Chunk> chunks = new();

        public ChunkArray()
        {
        }

        public Chunk this[int x, int y, int z]
        {
            get => Get(new(x, y, z));
            set => Set(new(x, y, z), value);
        }

        public Chunk this[Vector3 pos]
        {
            get => Get(new((int)pos.X, (int)pos.Y, (int)pos.Z));
            set => Set(new((int)pos.X, (int)pos.Y, (int)pos.Z), value);
        }

        public Chunk Get(Vector3 pos)
        {
            if (chunks.TryGetValue(pos, out Chunk chunk))
            {
                return chunk;
            }
            else
            {
                return null;
            }
        }

        public void Set(Vector3 pos, Chunk value)
        {
            chunks[pos] = value;
        }

        public void Remove(Vector3 pos)
        {
            chunks.TryRemove(pos, out _);
        }

        public void Remove(Chunk value)
        {
            chunks.TryRemove(new(value.Position, value));
        }

        public int Count()
        {
            return chunks.Count;
        }

        public void Clear()
        {
            chunks.Clear();
        }
    }
}