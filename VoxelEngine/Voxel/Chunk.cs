namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Diagnostics;
    using System.Numerics;
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

        public int DimId;
        public Vector3 Position;
        public BoundingBox BoundingBox;

        public BlockStorage Data = new(CHUNK_SIZE_CUBED);
        public byte* MinY;
        public byte* MaxY;

        public BlockMetadataCollection BlockMetadata = new();
        public BiomeMetadata BiomeMetadata = new();

        public ChunkVertexBuffer VertexBuffer = new();

        public ChunkHelper ChunkHelper;
        public Chunk? cXN, cXP, cYN, cYP, cZN, cZP;

        public bool HasMissingNeighbours;

        public readonly Lock _lock = new();

        private InternalChunkFlags flags;

        public Chunk(World map, int x, int y, int z, bool generated = false)
        {
            MinY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            ZeroMemoryT(MinY, CHUNK_SIZE_SQUARED);
            MaxY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            ZeroMemoryT(MaxY, CHUNK_SIZE_SQUARED);
            DimId = map.DimId;
            Position = new(x, y, z);

            Vector3 realPos = new Vector3(x, y, z) * CHUNK_SIZE;
            BoundingBox = new BoundingBox(realPos, realPos + new Vector3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
            Memset(MinY, CHUNK_SIZE, CHUNK_SIZE_SQUARED);
            DiskDirty = generated;
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

        public World Map => DimensionManager.GetWorld(DimId);

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

        public bool InMemory => Data.IsAllocated;

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

        public bool DiskDirty
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
                DimensionManager.GetWorld(DimId).Chunks.Remove(this);
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
                MinY = null;
                MaxY = null;
                BlockMetadata.Release();
            }
        }

        /// <summary>
        /// This will completely unload the chunk from cpu and gpu and physics
        /// </summary>
        public void Unload()
        {
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
                if (!Data.IsAllocated)
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
            }
        }

        public void SetBlockInternal(Block block, int x, int y, int z)
        {
            lock (_lock)
            {
                DiskDirty = true;
                Dirty = true;

                int index = Extensions.MapToIndex(x, y, z);
                Data[index] = block;

                int heightAccess = new Point2(x, z).MapToIndex();

                if (block.Type == 0)
                {
                    if (MaxY[heightAccess] == y + 1)
                    {
                        byte newMaxY = 0;
                        for (int yl = y; yl >= 0; yl--)
                        {
                            if (Data[new Point3(x, yl, z).MapToIndex()].Type != 0)
                            {
                                newMaxY = (byte)(yl + 1);
                                break;
                            }
                        }
                        MaxY[heightAccess] = newMaxY;
                    }

                    if (MinY[heightAccess] == y)
                    {
                        byte newMinY = 15;
                        for (int yl = y; yl <= 16; yl++)
                        {
                            if (Data[new Point3(x, yl, z).MapToIndex()].Type != 0)
                            {
                                newMinY = (byte)yl;
                                break;
                            }
                        }
                        MinY[heightAccess] = newMinY;
                    }
                }
                else
                {
                    MinY[heightAccess] = Math.Min(MinY[heightAccess], (byte)y);
                    MaxY[heightAccess] = Math.Max(MaxY[heightAccess], (byte)(y + 1));
                }
            }
        }

        public Block GetBlockInternal(int x, int y, int z)
        {
            {
                // Chunk data accessed quickly using bit masks
                int index = Extensions.MapToIndex(x, y, z);
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
            fixed (ChunkVertexBuffer* vertexBuffer = &VertexBuffer)
            {
                VoxelMeshFactory.GenerateMesh(vertexBuffer, this);
            }
        }

        public unsafe void Serialize(Stream stream)
        {
            lock (_lock)
            {
                DiskDirty = false;
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

        private string GetDebuggerDisplay()
        {
            var name = $"<{Position.X},{Position.Y},{Position.Z}>";
            return $"{name}, Flags: {flags}";
        }
    }
}