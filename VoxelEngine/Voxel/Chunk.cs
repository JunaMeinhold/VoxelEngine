//#define USE_LEGACY_LOADER

namespace VoxelEngine.Voxel
{
    using System.Buffers;
    using System.Buffers.Binary;
    using System.Collections.Concurrent;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Physics;
    using VoxelEngine.Scenes;

    public class Chunk : IDisposable
    {
        #region Fields

        public const int EMPTY = 0;
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_SIZE_SQUARED = 16 * 16;
        public const int CHUNK_SIZE_CUBED = 16 * 16 * 16;
        public const int CHUNK_SIZE_MINUS_ONE = 15;
        public const int CHUNK_SIZE_SHIFTED = 16 << 6;

        public WorldMap Map;

        public Block[] Data = new Block[CHUNK_SIZE_CUBED];
        public byte[] MinY = new byte[CHUNK_SIZE_SQUARED];
        public byte[] MaxY = new byte[CHUNK_SIZE_SQUARED];

#if USE_LEGACY_LOADER
        public BlockVertexBuffer VertexBuffer = new();
#else
        public ChunkVertexBuffer VertexBuffer = new();
#endif
        private ChunkHelper chunkHelper;
        public Chunk cXN, cXP, cYN, cYP, cZN, cZP;

        public Vector3 Position;
        public BoundingBox BoundingBox;
        public ChunkStaticHandle2 Handle;

        public string Name;

        public bool MissingNeighbours;

        #endregion Fields

        #region Constructor / Destructor

        public Chunk(WorldMap map, int x, int y, int z, bool generated = false)
        {
            Map = map;
            Position = new(x, y, z);
#if USE_LEGACY_LOADER
            VertexBuffer.DebugName = $"Chunk: {Position}";
#endif
            Vector3 realPos = new Vector3(x, y, z) * CHUNK_SIZE;
            BoundingBox = new BoundingBox(realPos, realPos + new Vector3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
            Array.Fill(MinY, (byte)CHUNK_SIZE);
            DirtyDisk = generated;
            Name = $"<{x},{y},{z}>";
        }

        ~Chunk()
        {
            Dispose();
        }

        #endregion Constructor / Destructor

        #region Properties

        public bool InBuffer { get; private set; }

        public bool InMemory => Data is not null;

        public bool InSimulation { get; private set; }

        public bool Dirty { get; private set; }

        public bool DirtyDisk { get; private set; }

        #endregion Properties

        #region Dispose

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

        public void Dispose()
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
            Data = null;
            MinY = null;
            MaxY = null;
            chunkHelper = null;
            GC.SuppressFinalize(this);
        }

        #endregion Dispose

        #region GPU / Update Stuff

        /// <summary>
        /// Generates the chunk mesh
        /// </summary>
        public void Update()
        {
            if (Data is null)
            {
                return;
            }

            chunkHelper = new();
            GenerateMesh();
            Dirty = false;
            chunkHelper = null;
        }

#if USE_LEGACY_LOADER
        /// <summary>
        /// Uploads the mesh to the gpu
        /// </summary>
        public void Upload(ID3D11Device device)
        {
            VertexBuffer?.BufferData(device);
            InBuffer = true;
        }
#endif

        /// <summary>
        /// Frees the memory on the gpu
        /// </summary>
        public void Unload()
        {
            InBuffer = false;
            VertexBuffer.Dispose();
            VertexBuffer = null;
        }

#if USE_LEGACY_LOADER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context)
        {
            VertexBuffer?.Bind(context);
        }
#endif

        #endregion GPU / Update Stuff

        #region Access Methods

        public void SetBlockInternal(Block block, int x, int y, int z)
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

        public Block GetBlockInternal(int x, int y, int z)
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

        public Block GetBlock(Vector3 pos)
        {
            Block block = GetBlockInternal((int)pos.X, (int)pos.Y, (int)pos.Z);
            return block;
        }

        public void SetBlock(Block block, Vector3 pos)
        {
            SetBlockInternal(block, (int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        #endregion Access Methods

        #region Meshing

        public bool UpdateNeighbours()
        {
            if (!MissingNeighbours)
            {
                return false;
            }

            cXN = Map.Chunks[(int)(Position.X - 1), (int)Position.Y, (int)Position.Z];
            if (cXN is not null && cXN.Data is null)
            {
                cXN = null;
            }

            // Positive X side
            cXP = Map.Chunks[(int)(Position.X + 1), (int)Position.Y, (int)Position.Z];
            if (cXP is not null && cXP.Data is null)
            {
                cXP = null;
            }

            // Negative Y side
            cYN = Position.Y > 0 ? Map.Chunks[(int)Position.X, (int)(Position.Y - 1), (int)Position.Z] : null;
            if (cYN is not null && cYN.Data is null)
            {
                cYN = null;
            }

            // Positive Y side
            cYP = Position.Y < WorldMap.CHUNK_AMOUNT_Y - 1 ? Map.Chunks[(int)Position.X, (int)(Position.Y + 1), (int)Position.Z] : null;
            if (cYP is not null && cYP.Data is null)
            {
                cYP = null;
            }

            // Negative Z neighbour
            cZN = Map.Chunks[(int)Position.X, (int)Position.Y, (int)(Position.Z - 1)];
            if (cZN is not null && cZN.Data is null)
            {
                cZN = null;
            }

            // Positive Z side
            cZP = Map.Chunks[(int)Position.X, (int)Position.Y, (int)(Position.Z + 1)];
            if (cZP is not null && cZP.Data is null)
            {
                cZP = null;
            }

            if (MissingNeighbours)
            {
                MissingNeighbours = cXN == null || cXP == null || cYN == null || cYP == null || cZN == null || cZP == null;
                return MissingNeighbours;
            }
            return false;
        }

        private void GenerateMesh()
        {
            // Default 4096, else use the lase size + 1024
            int newSize = VertexBuffer.Count == 0 ? 4096 : VertexBuffer.Count + 1024;
            VertexBuffer.Reset(newSize);

            // Negative X side

            cXN = Map.Chunks[(int)(Position.X - 1), (int)Position.Y, (int)Position.Z];
            if (cXN is not null && cXN.Data is null)
            {
                cXN = null;
            }

            // Positive X side
            cXP = Map.Chunks[(int)(Position.X + 1), (int)Position.Y, (int)Position.Z];
            if (cXP is not null && cXP.Data is null)
            {
                cXP = null;
            }

            // Negative Y side
            cYN = Position.Y > 0 ? Map.Chunks[(int)Position.X, (int)(Position.Y - 1), (int)Position.Z] : null;
            if (cYN is not null && cYN.Data is null)
            {
                cYN = null;
            }

            // Positive Y side
            cYP = Position.Y < WorldMap.CHUNK_AMOUNT_Y - 1 ? Map.Chunks[(int)Position.X, (int)(Position.Y + 1), (int)Position.Z] : null;
            if (cYP is not null && cYP.Data is null)
            {
                cYP = null;
            }

            // Negative Z neighbour
            cZN = Map.Chunks[(int)Position.X, (int)Position.Y, (int)(Position.Z - 1)];
            if (cZN is not null && cZN.Data is null)
            {
                cZN = null;
            }

            // Positive Z side
            cZP = Map.Chunks[(int)Position.X, (int)Position.Y, (int)(Position.Z + 1)];
            if (cZP is not null && cZP.Data is null)
            {
                cZP = null;
            }

            MissingNeighbours = cXN == null || cXP == null || cYN == null || cYP == null || cZN == null || cZP == null;

            // Precalculate the map-relative Y position of the chunk in the map
            int chunkY = (int)(Position.Y * CHUNK_SIZE);

            // Allocate variables on the stack
            int access, heightMapAccess, iCS, kCS2, i1, k1, j, topJ;
            bool minX, maxX, minZ, maxZ;

            k1 = 1;

            for (int k = 0; k < CHUNK_SIZE; k++, k1++)
            {
                // Calculate this once, rather than multiple times in the inner loop
                kCS2 = k * CHUNK_SIZE_SQUARED;

                i1 = 1;
                heightMapAccess = k * CHUNK_SIZE;

                // Is the current run on the Z- or Z+ edge of the chunk
                minZ = k == 0;
                maxZ = k == CHUNK_SIZE_MINUS_ONE;

                for (int i = 0; i < CHUNK_SIZE; i++, i1++)
                {
                    // Determine where to start the innermost loop
                    j = MinY[heightMapAccess];
                    topJ = MaxY[heightMapAccess];
                    heightMapAccess++;

                    // Calculate this once, rather than multiple times in the inner loop
                    iCS = i * CHUNK_SIZE;

                    // Calculate access here and increment it each time in the innermost loop
                    access = kCS2 + iCS + j;

                    // Is the current run on the X- or X+ edge of the chunk
                    minX = i == 0;
                    maxX = i == CHUNK_SIZE_MINUS_ONE;

                    // X and Z runs search upwards to create runs, so start at the bottom.
                    for (; j < topJ; j++, access++)
                    {
                        ref Block b = ref Data[access];

                        if (b.Type != EMPTY)
                        {
                            CreateRun(ref b, i, j, k << 12, i1, k1 << 12, j + chunkY, access, minX, maxX, j == 0, j == CHUNK_SIZE_MINUS_ONE, minZ, maxZ, iCS, kCS2);
                        }
                    }

                    // Extend the array if it is nearly full
                    if (VertexBuffer.Count > VertexBuffer.Capacity - 2048)
                    {
                        VertexBuffer.EnsureCapacity(VertexBuffer.Capacity + 2048);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateRun(ref Block b, int i, int j, int k, int i1, int k1, int y, int access, bool minX, bool maxX, bool minY, bool maxY, bool minZ, bool maxZ, int iCS, int kCS2)
        {
            int textureHealth16 = BlockVertex.IndexToTextureShifted[b.Type];
            int accessIncremented = access + 1;
            int chunkAccess;
            int j1 = j + 1;
            int jS = j << 6;
            int jS1 = j1 << 6;
            int length;

            // Left (X-)
            if (!chunkHelper.visitXN[access] && DrawFaceXN(j, access, minX, kCS2))
            {
                chunkHelper.visitXN[access] = true;
                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitXN[chunkAccess++] = true;
                }

                // k1 and k are already shifted
                BlockVertex.AppendQuadX(VertexBuffer, i, jS, length, k1, k, (int)FaceTypeShifted.XN, textureHealth16);
            }

            // Right (X+)
            if (!chunkHelper.visitXP[access] && DrawFaceXP(j, access, maxX, kCS2))
            {
                chunkHelper.visitXP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitXP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadX(VertexBuffer, i1, jS, length, k, k1, (int)FaceTypeShifted.XP, textureHealth16);
            }

            // Back (Z-)
            if (!chunkHelper.visitZN[access] && DrawFaceZN(j, access, minZ, iCS))
            {
                chunkHelper.visitZN[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitZN[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(VertexBuffer, i1, i, jS, length, k, (int)FaceTypeShifted.ZN, textureHealth16);
            }

            // Front (Z+)
            if (!chunkHelper.visitZP[access] && DrawFaceZP(j, access, maxZ, iCS))
            {
                chunkHelper.visitZP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitZP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(VertexBuffer, i, i1, jS, length, k1, (int)FaceTypeShifted.ZP, textureHealth16);
            }

            // Bottom (Y-)
            if (!chunkHelper.visitYN[access] && DrawFaceYN(access, minY, iCS, kCS2))
            {
                chunkHelper.visitYN[access] = true;

                chunkAccess = access + CHUNK_SIZE;

                for (length = i1; length < CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitYN[chunkAccess] = true;

                    chunkAccess += CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(VertexBuffer, i, length, jS, k1, k, (int)FaceTypeShifted.YN, textureHealth16);
            }

            // Top (Y+)
            if (!chunkHelper.visitYP[access] && DrawFaceYP(access, maxY, iCS, kCS2))
            {
                chunkHelper.visitYP[access] = true;

                chunkAccess = access + CHUNK_SIZE;

                for (length = i1; length < CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitYP[chunkAccess] = true;

                    chunkAccess += CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(VertexBuffer, i, length, jS1, k, k1, (int)FaceTypeShifted.YP, textureHealth16);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceXN(int j, int access, bool min, int kCS2)
        {
            if (min)
            {
                if (cXN == null)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return cXN.Data[CHUNK_SIZE_MINUS_ONE * CHUNK_SIZE + j + kCS2].Type == 0;
            }

            return Data[access - CHUNK_SIZE].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceXP(int j, int access, bool max, int kCS2)
        {
            if (max)
            {
                if (cXP == null)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return cXP.Data[j + kCS2].Type == 0;
            }

            return Data[access + CHUNK_SIZE].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceYN(int access, bool min, int iCS, int kCS2)
        {
            if (min)
            {
                if (Position.Y == 0)
                {
                    return true;
                }

                if (cYN == null)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return cYN.Data[iCS + CHUNK_SIZE_MINUS_ONE + kCS2].Type == 0;
            }

            return Data[access - 1].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceYP(int access, bool max, int iCS, int kCS2)
        {
            if (max)
            {
                // Don't check chunkYPos here as players can move above the map
                if (cYP == null)
                {
                    return true;
                }
                else
                {
                    return cYP.Data[iCS + kCS2].Type == 0;
                }
            }
            else
            {
                return Data[access + 1].Type == 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceZN(int j, int access, bool min, int iCS)
        {
            if (min)
            {
                if (cZN == null)
                {
                    return true;
                }

                return cZN.Data[iCS + j + CHUNK_SIZE_MINUS_ONE * CHUNK_SIZE_SQUARED].Type == 0;
            }

            return Data[access - CHUNK_SIZE_SQUARED].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceZP(int j, int access, bool max, int iCS)
        {
            if (max)
            {
                if (cZP == null)
                {
                    return true;
                }

                return cZP.Data[iCS + j].Type == 0;
            }

            return Data[access + CHUNK_SIZE_SQUARED].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DifferentBlock(int chunkAccess, ref Block compare)
        {
            ref Block b = ref Data[chunkAccess];
            return b.Type != compare.Type;
        }

        #endregion Meshing

        #region Serialization

        [StructLayout(LayoutKind.Sequential)]
        private struct ChunkRecord
        {
            public ushort Type;
            public Vector3 Position;

            public ChunkRecord(ushort type, Vector3 position)
            {
                Type = type;
                Position = position;
            }
        }

        public void SerializeTo(Stream stream)
        {
            DirtyDisk = false;
            ConcurrentQueue<ChunkRecord> savequeue = new();

            if (Data != null)
            {
                for (int k = 0; k < CHUNK_SIZE; k++)
                {
                    // Calculate this once, rather than multiple times in the inner loop
                    int kCS2 = k * CHUNK_SIZE_SQUARED;

                    int heightMapAccess = k * CHUNK_SIZE;

                    for (int i = 0; i < CHUNK_SIZE; i++)
                    {
                        // Determine where to start the innermost loop
                        int j = MinY[heightMapAccess];
                        int topJ = MaxY[heightMapAccess];
                        heightMapAccess++;

                        // Calculate this once, rather than multiple times in the inner loop
                        int iCS = i * CHUNK_SIZE;

                        // Calculate access here and increment it each time in the innermost loop
                        int access = kCS2 + iCS + j;

                        // X and Z runs search upwards to create runs, so start at the bottom.
                        for (; j < topJ; j++, access++)
                        {
                            ref Block b = ref Data[access];

                            if (b.Type != EMPTY)
                            {
                                savequeue.Enqueue(new ChunkRecord() { Position = new(i, j, k), Type = b.Type });
                            }
                        }
                    }
                }
            }

            int size = 4 + CHUNK_SIZE_SQUARED + CHUNK_SIZE_SQUARED + savequeue.Count * Marshal.SizeOf<ChunkRecord>();
            byte[] result = new byte[size];
            Span<byte> span = result.AsSpan(0, size);
            int index = 4;
            MinY.CopyTo(span[index..]);
            index += CHUNK_SIZE_SQUARED;
            MaxY.CopyTo(span[index..]);
            index += CHUNK_SIZE_SQUARED;
            byte[] buffer = new byte[Marshal.SizeOf<ChunkRecord>()];
            BinaryPrimitives.WriteInt32LittleEndian(span, savequeue.Count);
            while (savequeue.TryDequeue(out ChunkRecord run))
            {
                run.GetBytes(buffer).CopyTo(span[index..]);
                index += buffer.Length;
            }

            stream.Write(span);
        }

        public unsafe void SerializeToUnsafe(Stream stream)
        {
            DirtyDisk = false;

            long begin = stream.Position;

            stream.Position += 4;

            stream.Write(MinY);
            stream.Write(MaxY);

            int blocksWritten = 0;
            if (Data != null)
            {
                const int bufferSize = 64;
                Span<ChunkRecord> buffer = stackalloc ChunkRecord[bufferSize]; // 3 * 8 + 2 * 128 = 1664B
                Span<byte> binBuffer = MemoryMarshal.AsBytes(buffer);
                int bufI = 0;
                for (int k = 0; k < CHUNK_SIZE; k++)
                {
                    // Calculate this once, rather than multiple times in the inner loop
                    int kCS2 = k * CHUNK_SIZE_SQUARED;

                    int heightMapAccess = k * CHUNK_SIZE;

                    for (int i = 0; i < CHUNK_SIZE; i++)
                    {
                        // Determine where to start the innermost loop
                        int j = MinY[heightMapAccess];
                        int topJ = MaxY[heightMapAccess];
                        heightMapAccess++;

                        // Calculate this once, rather than multiple times in the inner loop
                        int iCS = i * CHUNK_SIZE;

                        // Calculate access here and increment it each time in the innermost loop
                        int access = kCS2 + iCS + j;

                        // X and Z runs search upwards to create runs, so start at the bottom.
                        for (; j < topJ; j++, access++)
                        {
                            ref Block b = ref Data[access];

                            if (b.Type != EMPTY)
                            {
                                buffer[bufI++] = new(b.Type, new(i, j, k));
                                blocksWritten++;

                                if (bufI == bufferSize)
                                {
                                    stream.Write(binBuffer);
                                    bufI = 0;
                                }
                            }
                        }
                    }
                }

                if (bufI != 0)
                {
                    stream.Write(binBuffer);
                    bufI = 0;
                }
            }

            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buf, blocksWritten);
            long end = stream.Position;
            stream.Position = begin;
            stream.Write(buf);
            stream.Position = end;
        }

        public int DeserializeFrom(Span<byte> span)
        {
            if (Data is null)
            {
                chunkHelper = new();
                Data = new Block[CHUNK_SIZE_CUBED];
                MinY = new byte[CHUNK_SIZE_SQUARED];
                MaxY = new byte[CHUNK_SIZE_SQUARED];
                Array.Fill(MinY, (byte)CHUNK_SIZE);
            }
            int index = 0;
            int count = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
            index += 4;
            span.Slice(index, CHUNK_SIZE_SQUARED).CopyTo(MinY);
            index += CHUNK_SIZE_SQUARED;
            span.Slice(index, CHUNK_SIZE_SQUARED).CopyTo(MaxY);
            index += CHUNK_SIZE_SQUARED;

            byte[] buffer = new byte[Marshal.SizeOf<ChunkRecord>()];
            for (int i = 0; i < count; i++)
            {
                span.Slice(index, buffer.Length).CopyTo(buffer);
                index += buffer.Length;
                ChunkRecord record = buffer.FromBytes<ChunkRecord>();
                Data[record.Position.MapToIndex(CHUNK_SIZE, CHUNK_SIZE)] = new Block() { Type = record.Type };
            }
            return index;
        }

        public unsafe int DeserializeFromUnsafe(byte* data, int length)
        {
            Span<byte> span = new(data, length);
            if (Data is null)
            {
                chunkHelper = new();
                Data = new Block[CHUNK_SIZE_CUBED];
                MinY = new byte[CHUNK_SIZE_SQUARED];
                MaxY = new byte[CHUNK_SIZE_SQUARED];
                Array.Fill(MinY, (byte)CHUNK_SIZE);
            }
            int index = 0;
            int count = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
            index += 4;
            span.Slice(index, CHUNK_SIZE_SQUARED).CopyTo(MinY);
            index += CHUNK_SIZE_SQUARED;
            span.Slice(index, CHUNK_SIZE_SQUARED).CopyTo(MaxY);
            index += CHUNK_SIZE_SQUARED;

            ChunkRecord* records = (ChunkRecord*)(data + index);
            for (int i = 0; i < count; i++, records++)
            {
                ChunkRecord record = *records;
                Data[record.Position.MapToIndex(CHUNK_SIZE, CHUNK_SIZE)] = new Block(record.Type);
            }
            return index + count * sizeof(ChunkRecord);
        }

        #endregion Serialization

        #region Physics

        public void LoadToSimulation()
        {
            if (InSimulation)
            {
                return;
            }

            Handle = new(SceneManager.Current.Simulation, SceneManager.Current.BufferPool, this);
            InSimulation = true;
        }

        public void UnloadFormSimulation()
        {
            if (!InSimulation)
            {
                return;
            }

            Handle.Free(SceneManager.Current.Simulation, SceneManager.Current.BufferPool);
            InSimulation = false;
        }

        #endregion Physics
    }
}