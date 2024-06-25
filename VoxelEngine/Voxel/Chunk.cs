namespace VoxelEngine.Voxel
{
    using System.Diagnostics;
    using System.Numerics;
    using BepuUtilities.Memory;
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

        public Block* Data = AllocTAndZero<Block>(CHUNK_SIZE_CUBED);
        public byte* MinY = AllocTAndZero<byte>(CHUNK_SIZE_SQUARED);
        public byte* MaxY = AllocTAndZero<byte>(CHUNK_SIZE_SQUARED);

        public BlockMetadataCollection BlockMetadata = new();
        public BiomeMetadata BiomeMetadata = new();

        public ChunkVertexBuffer VertexBuffer = new();
        public ChunkStaticHandle2 Handle;

        public ChunkHelper ChunkHelper;
        public Chunk cXN, cXP, cYN, cYP, cZN, cZP;

        public bool HasMissingNeighbours;

        public readonly object _lock = new();

        public Chunk(WorldMap map, int x, int y, int z, bool generated = false)
        {
            Map = map;
            Position = new(x, y, z);
#if USE_LEGACY_LOADER
            VertexBuffer.DebugName = $"Chunk: {Position}";
#endif
            Vector3 realPos = new Vector3(x, y, z) * CHUNK_SIZE;
            BoundingBox = new BoundingBox(realPos, realPos + new Vector3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
            Memset(MinY, CHUNK_SIZE, CHUNK_SIZE_SQUARED);
            DirtyDisk = generated;
            Name = $"<{x},{y},{z}>";
        }

        public bool InBuffer { get; private set; }

        public bool InMemory => Data is not null;

        public bool InSimulation { get; private set; }

        public bool Dirty { get; private set; }

        public bool DirtyDisk { get; private set; }

        private void RemoveRefsFrom(Chunk chunk)
        {
            lock (_lock)
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
                Free(Data);
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
                VertexBuffer.Dispose();
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
            lock (_lock)
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

        public unsafe int Serialize(Stream stream)
        {
            lock (_lock)
            {
                DirtyDisk = false;
                return ChunkSerializer.Serialize(stream, this);
            }
        }

        public unsafe int Deserialize(byte* data, int length)
        {
            lock (_lock)
            {
                return ChunkSerializer.Deserialize(this, data, length);
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
            return Name;
        }
    }
}