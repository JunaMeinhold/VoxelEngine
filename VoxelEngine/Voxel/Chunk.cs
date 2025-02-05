namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Diagnostics;
    using System.Numerics;
    using VoxelEngine.Voxel.Meshing;
    using VoxelEngine.Voxel.Metadata;
    using VoxelEngine.Voxel.Serialization;

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public unsafe struct Chunk
    {
        public const int EMPTY = 0;
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_SIZE_SQUARED = 16 * 16;
        public const int CHUNK_SIZE_CUBED = 16 * 16 * 16;
        public const int CHUNK_SIZE_MINUS_ONE = 15;
        public const int CHUNK_SIZE_SHIFTED = 16 << 6;

        public int DimId;
        public Point3 Position;
        public BoundingBox BoundingBox;

        public BlockStorage Data = new(CHUNK_SIZE_CUBED);
        public byte* MinY;
        public byte* MaxY;

        public BlockMetadataCollection BlockMetadata = new();
        public BiomeMetadata BiomeMetadata = new();

        public ChunkVertexBuffer VertexBuffer = new();

        public ChunkHelper ChunkHelper;

        public SemaphoreLight _lock = new(1, 1);

        private InternalChunkFlags flags;

        private int refCount = 1;

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

        public void AddRef()
        {
            Interlocked.Increment(ref refCount);
        }

        public void Dispose(Chunk* self)
        {
            int count = Interlocked.Decrement(ref refCount);
            if (count != 0) return;

            ChunkAllocator.Free(self);
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

        public readonly World Map => DimensionManager.GetWorld(DimId);

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

        public void FreeMemory()
        {
            InBuffer = false;
            VertexBuffer.Dispose();
            Data.Dispose();
            if (MinY != null)
            {
                Free(MinY);
                MinY = null;
            }
            if (MaxY != null)
            {
                Free(MaxY);
                MaxY = null;
            }
            BlockMetadata.Release();
        }

        public void Unload(Chunk* self)
        {
            _lock.Wait();
            try
            {
                InBuffer = false;
                VertexBuffer.Dispose();

                DimensionManager.GetWorld(DimId).Chunks.Remove(self);

                Data.Dispose();

                if (MinY != null)
                {
                    Free(MinY);
                    MinY = null;
                }

                if (MaxY != null)
                {
                    Free(MaxY);
                    MaxY = null;
                }

                BlockMetadata.Release();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Generates the chunk mesh
        /// </summary>
        public void Update(Chunk* self)
        {
            _lock.Wait();
            try
            {
                if (!Data.IsAllocated)
                {
                    return;
                }

                ChunkHelper = new();
                VoxelMeshFactory.GenerateMesh(self);
                Dirty = false;
                ChunkHelper.Release();
                ChunkHelper = default;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void SetBlockInternal(Block block, int x, int y, int z)
        {
            _lock.Wait();
            try
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
            finally
            {
                _lock.Release();
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

        public unsafe void Serialize(Chunk* self, Stream stream)
        {
            _lock.Wait();
            try
            {
                DiskDirty = false;
                ChunkSerializer.Serialize(self, stream);
            }
            finally
            {
                _lock.Release();
            }
        }

        public unsafe void Deserialize(Chunk* self, Stream stream)
        {
            _lock.Wait();
            try
            {
                ChunkSerializer.Deserialize(self, stream);
            }
            finally
            {
                _lock.Release();
            }
        }

        private string GetDebuggerDisplay()
        {
            var name = $"<{Position.X},{Position.Y},{Position.Z}>";
            return $"{name}, Flags: {flags}";
        }
    }
}