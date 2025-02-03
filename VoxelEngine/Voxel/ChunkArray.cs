namespace VoxelEngine.Voxel
{
    using System.Numerics;

    public class ChunkArray
    {
        private readonly Dictionary<Vector3, Chunk> chunks = new();
        private readonly Lock _lock = new();

        public ChunkArray()
        {
        }

        public Chunk? this[int x, int y, int z]
        {
            get => Get(new(x, y, z));
            set => Set(new(x, y, z), value);
        }

        public Chunk? this[Vector3 pos]
        {
            get => Get(new((int)pos.X, (int)pos.Y, (int)pos.Z));
            set => Set(new((int)pos.X, (int)pos.Y, (int)pos.Z), value);
        }

        public Chunk? Get(Vector3 pos)
        {
            lock (_lock)
            {
                if (chunks.TryGetValue(pos, out Chunk? chunk))
                {
                    return chunk;
                }
                else
                {
                    return null;
                }
            }
        }

        public void Set(Vector3 pos, Chunk? value)
        {
            lock (_lock)
            {
                if (value == null)
                {
                    chunks.Remove(pos);
                }
                else
                {
                    chunks[pos] = value;
                }
            }
        }

        public void Remove(Vector3 pos)
        {
            lock (_lock)
            {
                chunks.Remove(pos, out _);
            }
        }

        public void Remove(Chunk value)
        {
            lock (_lock)
            {
                chunks.Remove(value.Position);
            }
        }

        public int Count()
        {
            lock (_lock)
            {
                return chunks.Count;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                chunks.Clear();
            }
        }
    }
}