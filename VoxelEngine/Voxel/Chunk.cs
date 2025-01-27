namespace VoxelEngine.Voxel
{
    using System.Diagnostics;
    using System.Numerics;
    using BepuUtilities.Memory;
    using Hexa.NET.Mathematics;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Physics;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel.Meshing;
    using VoxelEngine.Voxel.Metadata;

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public unsafe class Chunk
    {
        public const int EMPTY = 0;
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_SIZE_SQUARED = 16 * 16;
        public const int CHUNK_SIZE_CUBED = 16 * 16 * 16;
        public const int CHUNK_SIZE_MINUS_ONE = 15;
        public const int CHUNK_SIZE_SHIFTED = 16 << 6;

        public string Name;
        public WorldMap Map;
        public Vector3 Position;
        public BoundingBox BoundingBox;

        public BlockStorage Data = new(0, CHUNK_SIZE_CUBED);
        public byte* MinY;
        public byte* MaxY;

        public BlockMetadataCollection BlockMetadata = new();
        public BiomeMetadata BiomeMetadata = new();

        public ChunkVertexBuffer VertexBuffer = new();
        public ChunkStaticHandle2 Handle;

        public ChunkHelper ChunkHelper;
        public Chunk? cXN, cXP, cYN, cYP, cZN, cZP;

        public bool HasMissingNeighbours;

        public readonly object _lock = new();

        private InternalChunkFlags flags;

        public Chunk(WorldMap map, int x, int y, int z, bool generated = false)
        {
            MinY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            ZeroMemoryT(MinY, CHUNK_SIZE_SQUARED);
            MaxY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            ZeroMemoryT(MaxY, CHUNK_SIZE_SQUARED);
            Map = map;
            Position = new(x, y, z);

            Vector3 realPos = new Vector3(x, y, z) * CHUNK_SIZE;
            BoundingBox = new BoundingBox(realPos, realPos + new Vector3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
            Memset(MinY, CHUNK_SIZE, CHUNK_SIZE_SQUARED);
            DirtyDisk = generated;
            Name = $"<{x},{y},{z}>";
        }

        private enum InternalChunkFlags : byte
        {
            None = 0,
            InBuffer = 1,
            InMemory = 2,
            InSimulation = 4,
            Dirty = 8,
            DiskDirty = 16,
        }

        public bool InBuffer
        {
            get => (flags & InternalChunkFlags.InBuffer) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.InBuffer;
                }
                else
                {
                    flags &= ~InternalChunkFlags.InBuffer;
                }
            }
        }

        public bool InMemory => Data is not null;

        public bool InSimulation
        {
            get => (flags & InternalChunkFlags.InSimulation) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.InSimulation;
                }
                else
                {
                    flags &= ~InternalChunkFlags.InSimulation;
                }
            }
        }

        public bool Dirty
        {
            get => (flags & InternalChunkFlags.Dirty) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.Dirty;
                }
                else
                {
                    flags &= ~InternalChunkFlags.Dirty;
                }
            }
        }

        public bool DirtyDisk
        {
            get => (flags & InternalChunkFlags.DiskDirty) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.DiskDirty;
                }
                else
                {
                    flags &= ~InternalChunkFlags.DiskDirty;
                }
            }
        }

        private void RemoveRefsFrom(Chunk chunk)
        {
            if (cXN == chunk)
            {
                cXN = null;
            }

            if (cXP == chunk)
            {
                cXP = null;
            }

            if (cYN == chunk)
            {
                cYN = null;
            }

            if (cYP == chunk)
            {
                cYP = null;
            }

            if (cZN == chunk)
            {
                cZN = null;
            }

            if (cZP == chunk)
            {
                cZP = null;
            }
        }

        public void UnloadFromMem()
        {
            lock (_lock)
            {
                Map.Chunks.Remove(this);
                Map = null;
                cXN?.RemoveRefsFrom(this);
                cXP?.RemoveRefsFrom(this);
                cYN?.RemoveRefsFrom(this);
                cYP?.RemoveRefsFrom(this);
                cZN?.RemoveRefsFrom(this);
                cZP?.RemoveRefsFrom(this);
                cXN = cXP = cYN = cYP = cZN = cZP = null;
                Data.Dispose();
                Free(MinY);
                Free(MaxY);
                Data = null;
                MinY = null;
                MaxY = null;
            }
        }

        /// <summary>
        /// This will completely unload the chunk from cpu and gpu and physics
        /// </summary>
        public void Unload()
        {
            UnloadFormSimulation();
            UnloadFromGPU();
            UnloadFromMem();
        }

        /// <summary>
        /// Generates the chunk mesh
        /// </summary>
        public void Update()
        {
            lock (_lock)
            {
                if (Data is null)
                {
                    return;
                }

                ChunkHelper = new();
                GenerateMesh();
                Dirty = false;
                ChunkHelper.Release();
                ChunkHelper = default;
            }
        }

        /// <summary>
        /// Frees the memory on the gpu
        /// </summary>
        public void UnloadFromGPU()
        {
            lock (_lock)
            {
                InBuffer = false;
                VertexBuffer?.Dispose();
                VertexBuffer = null;
            }
        }

        public void SetBlockInternal(Block block, int x, int y, int z)
        {
            lock (_lock)
            {
                DirtyDisk = true;
                Dirty = true;
                // Chunk data accessed quickly using bit masks
                int index = Extensions.MapToIndex(x, y, z, CHUNK_SIZE, CHUNK_SIZE);
                Data[index] = block;

                // Could be made better but for now it is okay.
                byte min = 0;
                byte max = 0;
                for (byte i = 0; i < CHUNK_SIZE; i++)
                {
                    int j = Extensions.MapToIndex(x, i, z, CHUNK_SIZE, CHUNK_SIZE);
                    if (i == min)
                    {
                        if (Data[j].Type == 0)
                        {
                            min++;
                        }
                    }
                    else
                    {
                        if (Data[j].Type != 0)
                        {
                            max = i;
                        }
                    }
                }
                max++;

                MinY[new Vector2(x, z).MapToIndex(CHUNK_SIZE)] = min;
                MaxY[new Vector2(x, z).MapToIndex(CHUNK_SIZE)] = max;
            }
        }

        public Block GetBlockInternal(int x, int y, int z)
        {
            {
                // Chunk data accessed quickly using bit masks
                int index = Extensions.MapToIndex(x, y, z, CHUNK_SIZE, CHUNK_SIZE);
                if (index < CHUNK_SIZE_CUBED)
                {
                    return Data[index];
                }
                else
                {
                    return default;
                }
            }
        }

        public Block GetBlock(Vector3 pos)
        {
            Block block = GetBlockInternal((int)pos.X, (int)pos.Y, (int)pos.Z);
            return block;
        }

        public void SetBlock(Block block, Vector3 pos)
        {
            SetBlockInternal(block, (int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        private void GenerateMesh()
        {
            VoxelMeshFactory.GenerateMesh(VertexBuffer, this);
        }

        public unsafe void Serialize(Stream stream)
        {
            lock (_lock)
            {
                DirtyDisk = false;
                ChunkSerializer.Serialize(stream, this);
            }
        }

        public unsafe void Deserialize(Stream stream)
        {
            lock (_lock)
            {
                ChunkSerializer.Deserialize(this, stream);
            }
        }

        public void LoadToSimulation(BufferPool pool)
        {
            lock (_lock)
            {
                if (InSimulation)
                {
                    return;
                }

                Handle = new(SceneManager.Current.Simulation, pool, this);
                InSimulation = true;
            }
        }

        public void UnloadFormSimulation()
        {
            lock (_lock)
            {
                if (!InSimulation)
                {
                    return;
                }

                Handle.Free(SceneManager.Current.Simulation);
                InSimulation = false;
            }
        }

        private string GetDebuggerDisplay()
        {
            return $"{Name}, Flags: {flags}";
        }
    }
}