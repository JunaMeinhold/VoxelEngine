namespace VoxelEngine.Voxel
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using BepuUtilities.Memory;
    using VoxelEngine.IO;

    public struct ChunkSegment
    {
        public Vector2 Position;
        public Chunk[] Chunks;

        public readonly bool IsEmpty => Chunks is null || Chunks[0] is null;

        public readonly bool IsLoaded => Chunks is not null && Chunks[0] is not null && Chunks[0].InBuffer;

        public readonly bool InMemory => Chunks is not null && Chunks[0] is not null && Chunks[0].InMemory;

        public readonly bool ExistOnDisk(WorldMap world)
        {
            return File.Exists(Path.Combine(world.Path, $"r.{Position.X}.{Position.Y}.vxr"));
        }

        public void Generate(World world)
        {
            Chunks = world.Generator.GenerateBatch(world, new(Position.X, 0, Position.Y));
        }

        public readonly void Load(BufferPool pool)
        {
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.Update();
                chunk.LoadToSimulation(pool);
            }
        }

        public readonly void UnloadFromMem()
        {
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.UnloadFromMem();
            }
        }

        public readonly void UnloadFromGPU()
        {
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.UnloadFromGPU();
            }
        }

        public readonly void UnloadFromSimulation()
        {
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.UnloadFormSimulation();
            }
        }

        /// <summary>
        /// This will completely unload the chunks from cpu / gpu / physics
        /// </summary>
        public readonly void Unload()
        {
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.Unload();
            }
        }

        public readonly void SaveToDisk()
        {
            SaveToDisk(Chunks[0].Map);
        }

        public readonly unsafe void SaveToDisk(WorldMap world)
        {
            bool dirty = false;
            for (int i = 0; i < Chunks.Length; i++)
            {
                if (Chunks[i].DirtyDisk)
                {
                    dirty = true;
                    break;
                }
            }

            if (!dirty)
            {
                return;
            }

            string filename = Path.Combine(world.Path, $"r.{Position.X}.{Position.Y}.vxr");
            FileStream fs = File.Create(filename);
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, Chunks.Length);
            fs.Write(buffer);
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.Serialize(fs);
            }
            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public unsafe void LoadFromDisk(WorldMap world)
        {
            string filename = Path.Combine(world.Path, $"r.{Position.X}.{Position.Y}.vxr");
            FileStream fs = File.OpenRead(filename);

            int count = fs.ReadInt32();

            Chunks = new Chunk[count];

            for (int i = 0; i < count; i++)
            {
                Chunk chunk = new(world, (int)Position.X, i, (int)Position.Y);
                chunk.Deserialize(fs);
                Chunks[i] = chunk;
                world.Chunks[Chunks[i].Position] = chunk;
            }

            world.Set(this);
            fs.Close();
            fs.Dispose();
        }

        public static ChunkSegment CreateFrom(WorldMap world, Vector3 pos)
        {
            Chunk[] chunks = new Chunk[WorldMap.CHUNK_AMOUNT_Y];
            for (int y = 0; y < WorldMap.CHUNK_AMOUNT_Y; y++)
            {
                chunks[y] = world.Get(new Vector3(pos.X, y, pos.Z));
            }

            return new() { Chunks = chunks, Position = new Vector2(pos.X, pos.Z) };
        }

        public static ChunkSegment CreateFrom(WorldMap world, float x, float z)
        {
            Chunk[] chunks = new Chunk[WorldMap.CHUNK_AMOUNT_Y];
            for (int i = 0; i < WorldMap.CHUNK_AMOUNT_Y; i++)
            {
                chunks[i] = world.Get(new Vector3(x, i, z));
            }

            return new() { Chunks = chunks, Position = new Vector2(x, z) };
        }

        public static bool operator ==(ChunkSegment left, ChunkSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkSegment left, ChunkSegment right)
        {
            return !(left == right);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is ChunkSegment region)
            {
                return region.Position == Position;
            }

            return false;
        }

        public override readonly int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}