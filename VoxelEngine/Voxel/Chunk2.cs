namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Voxel.Meshing;
    using VoxelEngine.Voxel.Metadata;

    public unsafe struct Chunk2
    {
        public const int EMPTY = 0;
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_SIZE_SQUARED = 16 * 16;
        public const int CHUNK_SIZE_CUBED = 16 * 16 * 16;
        public const int CHUNK_SIZE_MINUS_ONE = 15;
        public const int CHUNK_SIZE_SHIFTED = 16 << 6;

        public Vector3 Position;
        public byte* MinY;
        public byte* MaxY;
        public Block* Voxels;

        public ChunkVertexBuffer2 VertexBuffer;

        public BlockMetadataCollection BlockMetadata;
        public BiomeMetadata BiomeMetadata;

        private InternalChunkFlags flags;

        private enum InternalChunkFlags : byte
        {
            None = 0,
            InBuffer = 1,
            InMemory = 2,
            InSimulation = 4,
            Dirty = 8,
            DiskDirty = 16,
        }

        public readonly bool IsAllocated => Voxels == null;

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

        public void Allocate()
        {
            MinY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            Memset(MinY, CHUNK_SIZE, CHUNK_SIZE_SQUARED);
            MaxY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            ZeroMemoryT(MaxY, CHUNK_SIZE_SQUARED);
            Voxels = AllocT<Block>(CHUNK_SIZE_CUBED);
            ZeroMemoryT(Voxels, CHUNK_SIZE_CUBED);
        }

        public void Release()
        {
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
            if (Voxels != null)
            {
                Free(Voxels);
                Voxels = null;
            }
        }

        public void SetBlockInternal(Block block, int x, int y, int z)
        {
            DirtyDisk = true;
            Dirty = true;

            int index = Extensions.MapToIndex(x, y, z);
            Voxels[index] = block;

            int heightAccess = new Point2(x, z).MapToIndex();

            if (block.Type == 0)
            {
                if (MaxY[heightAccess] == y + 1)
                {
                    byte newMaxY = 0;
                    for (int yl = y; yl >= 0; yl--)
                    {
                        if (Voxels[new Point3(x, yl, z).MapToIndex()].Type != 0)
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
                        if (Voxels[new Point3(x, yl, z).MapToIndex()].Type != 0)
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

        public Block GetBlockInternal(int x, int y, int z)
        {
            // Chunk data accessed quickly using bit masks
            int index = Extensions.MapToIndex(x, y, z);
            if (index < CHUNK_SIZE_CUBED)
            {
                return Voxels[index];
            }
            else
            {
                return default;
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

        public void UnloadFromMem()
        {
            Release();
            BlockMetadata.Release();
        }

        public void UnloadFromGPU()
        {
            InBuffer = false;
            VertexBuffer.Dispose();
        }

        public void Unload()
        {
            UnloadFromGPU();
            UnloadFromMem();
        }

        public void Update()
        {
            if (!IsAllocated)
            {
                return;
            }

            GenerateMesh();
            Dirty = false;
        }

        private void GenerateMesh()
        {
            //VoxelMeshFactory.GenerateMesh(VertexBuffer, this);
        }
    }
}