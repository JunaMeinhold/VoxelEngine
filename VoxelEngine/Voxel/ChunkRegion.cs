namespace VoxelEngine.Voxel
{
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Vortice.Direct3D11;

    public struct ChunkRegion
    {
        public Vector2 Position;
        public Chunk[] Chunks;

        public readonly bool IsEmpty => Chunks is null || Chunks[0] is null;

        public readonly bool IsLoaded => Chunks is not null && Chunks[0] is not null && Chunks[0].InBuffer;

        public readonly bool InMemory => Chunks is not null && Chunks[0] is not null && Chunks[0].InMemory;

        public override readonly bool Equals(object obj)
        {
            if (obj is ChunkRegion region)
            {
                return region.Position == Position;
            }

            return false;
        }

        public readonly bool ExistOnDisk(WorldMap world)
        {
            return File.Exists(Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}"));
        }

        public void Generate(World world)
        {
            Chunks = world.Generator.GenerateBatch(world, new(Position.X, 0, Position.Y));
            Flush(Chunks[0].Map);
        }

#if USE_LEGACY_LOADER
        public readonly void Upload(ID3D11Device device)
        {
            foreach (Chunk chunk in Chunks)
            {
                chunk?.Upload(device);
            }
        }
#endif

        public readonly void Load()
        {
            _ = Parallel.ForEach(Chunks, chunk => chunk?.Update());
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.LoadToSimulation();
            }
        }

        public readonly void Unload()
        {
            foreach (Chunk chunk in Chunks)
            {
                chunk.Unload();
            }
        }

        public readonly void Save()
        {
            ToDiskUnsafe(Chunks[0].Map);
        }

        public readonly void UnloadFromSimulation()
        {
            foreach (Chunk chunk in Chunks)
            {
                chunk.UnloadFormSimulation();
            }
        }

        public readonly void ToDisk(WorldMap world)
        {
            if (Chunks.All(x => !x.DirtyDisk))
            {
                for (int i = 0; i < Chunks.Length; i++)
                {
                    Chunk chunk = Chunks[i];
                    chunk.Dispose();
                }

                return;
            }
            string filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            FileStream fs = File.Create(filename);
            fs.Write(BitConverter.GetBytes(Chunks.Length));
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.SerializeTo(fs);
                chunk.Dispose();
            }
            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public readonly unsafe void ToDiskUnsafe(WorldMap world)
        {
            if (Chunks.All(x => !x.DirtyDisk))
            {
                for (int i = 0; i < Chunks.Length; i++)
                {
                    Chunk chunk = Chunks[i];
                    chunk.Dispose();
                }

                return;
            }
            string filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            FileStream fs = File.Create(filename);
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, Chunks.Length);
            fs.Write(buffer);
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.SerializeToUnsafe(fs);
                chunk.Dispose();
            }
            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public readonly void Flush(WorldMap world)
        {
            string filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            FileStream fs = File.Create(filename);
            fs.Write(BitConverter.GetBytes(Chunks.Length));
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.SerializeTo(fs);
            }

            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public readonly unsafe void FlushUnsafe(WorldMap world)
        {
            string filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            FileStream fs = File.Create(filename);
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, Chunks.Length);
            fs.Write(buffer);
            for (int i = 0; i < Chunks.Length; i++)
            {
                Chunk chunk = Chunks[i];
                chunk.SerializeToUnsafe(fs);
            }

            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public void LoadFromDisk(WorldMap world)
        {
            string filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            FileStream fs = File.OpenRead(filename);
            byte[] data = ArrayPool<byte>.Shared.Rent((int)fs.Length);
            Span<byte> span = data.AsSpan(0, (int)fs.Length);
            _ = fs.Read(span);
            int index = 0;
            int count = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
            index += 4;
            Chunks = new Chunk[count];

            for (int i = 0; i < count; i++)
            {
                Chunks[i] = new(world, (int)Position.X, i, (int)Position.Y);
                index += Chunks[i].DeserializeFrom(span[index..]);
                world.Chunks[Chunks[i].Position] = Chunks[i];
            }
            world.Set(this);
            fs.Close();
            fs.Dispose();
            ArrayPool<byte>.Shared.Return(data);
        }

        public unsafe void LoadFromDiskUnsafe(WorldMap world)
        {
            string filename = Path.Combine(world.Path, $"region-{Position.X}-{Position.Y}");
            FileStream fs = File.OpenRead(filename);
            byte* data = (byte*)Marshal.AllocHGlobal((nint)fs.Length);
            Span<byte> span = new(data, (int)fs.Length);
            _ = fs.Read(span);
            int index = 0;
            int count = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
            index += 4;
            Chunks = new Chunk[count];

            for (int i = 0; i < count; i++)
            {
                Chunks[i] = new(world, (int)Position.X, i, (int)Position.Y);
                index += Chunks[i].DeserializeFromUnsafe(data + index, (int)(fs.Length - index));
                world.Chunks[Chunks[i].Position] = Chunks[i];
            }
            world.Set(this);
            fs.Close();
            fs.Dispose();
            Marshal.FreeHGlobal((nint)data);
        }

        public static ChunkRegion CreateFrom(WorldMap world, Vector3 pos)
        {
            Chunk[] chunks = new Chunk[WorldMap.CHUNK_AMOUNT_Y];
            for (int y = 0; y < WorldMap.CHUNK_AMOUNT_Y; y++)
            {
                chunks[y] = world.Get(new Vector3(pos.X, y, pos.Z));
            }

            return new() { Chunks = chunks, Position = new Vector2(pos.X, pos.Z) };
        }

        public static ChunkRegion CreateFrom(WorldMap world, float x, float z)
        {
            Chunk[] chunks = new Chunk[WorldMap.CHUNK_AMOUNT_Y];
            for (int i = 0; i < WorldMap.CHUNK_AMOUNT_Y; i++)
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

        public override readonly int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}