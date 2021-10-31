namespace HexaEngine.Objects.VoxelGen
{
    using HexaEngine.Objects;
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using System.IO;
    using System.Numerics;
    using System.Threading.Tasks;

    public struct ChunkRegion
    {
        public Vector2 Position;
        public Chunk[] Chunks;

        public bool IsEmpty => Chunks is null || Chunks[0] is null;

        public bool IsLoaded => Chunks is not null && Chunks[0] is not null && Chunks[0].IsLoaded;

        public bool InMemory => Chunks is not null && Chunks[0] is not null && Chunks[0].InMemory;

        public override bool Equals(object obj)
        {
            if (obj is ChunkRegion region)
            {
                return region.Position == Position;
            }
            return false;
        }

        public bool ExistOnDisk(WorldMap world)
        {
            return File.Exists(Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}"));
        }

        public void Update()
        {
            _ = Parallel.ForEach(Chunks, chunk => chunk?.Update());
        }

        public void Generate(World world)
        {
            Chunks = world.Generator.GenerateBatch(world, new(Position.X, 0, Position.Y));
        }

        public void Upload()
        {
            foreach (var chunk in Chunks)
            {
                chunk?.Upload();
            }
        }

        public void Unload()
        {
            foreach (var chunk in Chunks)
            {
                chunk.Unload();
            }
        }

        public void DeepUnload()
        {
            foreach (var chunk in Chunks)
            {
                chunk.Unload();
            }
            ToDisk(Chunks[0].Map);
        }

        public void ToDisk(WorldMap world)
        {
            var filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            var fs = File.Create(filename);
            fs.Write(BitConverter.GetBytes(Chunks.Length));
            foreach (var chunk in Chunks)
            {
                chunk.SerializeTo(fs);
            }
            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public void LoadFromDisk(WorldMap world)
        {
            var filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            var fs = File.OpenRead(filename);
            var data = ArrayPool<byte>.Shared.Rent((int)fs.Length);
            var span = data.AsSpan(0, (int)fs.Length);
            _ = fs.Read(span);
            var index = 0;
            var count = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
            index += 4;
            Chunks = new Chunk[count];

            for (int i = 0; i < count; i++)
            {
                Chunks[i] = new(world, (int)Position.X, i, (int)Position.Y);
                index += Chunks[i].DeserializeFrom(span[index..]);
            }
            world.Set(this);
            fs.Close();
            fs.Dispose();
            ArrayPool<byte>.Shared.Return(data);
        }

        public static ChunkRegion CreateFrom(WorldMap world, Vector3 pos)
        {
            var chunks = new Chunk[WorldMap.MAP_SIZE_Y];
            for (int y = 0; y < WorldMap.MAP_SIZE_Y; y++)
            {
                chunks[y] = world.Get(new Vector3(pos.X, y, pos.Z));
            }
            return new() { Chunks = chunks, Position = new Vector2(pos.X, pos.Z) };
        }

        public static ChunkRegion CreateFrom(WorldMap world, float x, float z)
        {
            var chunks = new Chunk[WorldMap.MAP_SIZE_Y];
            for (int i = 0; i < WorldMap.MAP_SIZE_Y; i++)
            {
                chunks[i] = world.Get(new Vector3(x, i, z));
            }
            return new() { Chunks = chunks, Position = new Vector2(x, z) };
        }

        public static bool operator ==(ChunkRegion left, ChunkRegion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkRegion left, ChunkRegion right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}