using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace HexaEngine.Objects.VoxelGen
{
    public class Chunk
    {
        public const int EMPTY = 0;
        public const int CHUNK_SIZE = 32;
        public const int CHUNK_SIZE_SQUARED = 1024;
        public const int CHUNK_SIZE_CUBED = 32768;
        public const int CHUNK_SIZE_MINUS_ONE = 31;
        public const int CHUNK_SIZE_SHIFTED = 32 << 6;

        public Block[] data = new Block[CHUNK_SIZE_CUBED];

        public BlockVertexBuffer vertexBuffer = new();

        // Parent reference to access blocks in other chunks
        public WorldMap Map;

        // The position of this chunk in the chunk grid.
        // Maps are usually 16 chunks wide, 16 chunks long and 6 chunks tall
        public int chunkPosX, chunkPosY, chunkPosZ;

        // Height maps
        public byte[] MinY = new byte[CHUNK_SIZE_SQUARED];

        public byte[] MaxY = new byte[CHUNK_SIZE_SQUARED];

        private ChunkHelper chunkHelper;
        private Chunk cXN, cXP, cYN, cYP, cZN, cZP;

        public Chunk(WorldMap map, int x, int y, int z)
        {
            Map = map;
            chunkPosX = x;
            chunkPosY = y;
            chunkPosZ = z;
            var realPos = new Vector3(x, y, z) * CHUNK_SIZE;
            BoundingBox = new BoundingBox(realPos, realPos + new Vector3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
            Array.Fill(MinY, (byte)CHUNK_SIZE);
        }

        public bool IsLoaded { get; private set; }

        public bool InMemory => data is not null;

        public BoundingBox BoundingBox { get; set; }

        /// <summary>
        /// Frees the memory on the gpu
        /// </summary>
        public void Unload()
        {
            IsLoaded = false;
            vertexBuffer.Unload();
            vertexBuffer.Reset(0);
        }

        /// <summary>
        /// Generates the chunk mesh
        /// </summary>
        public void Update()
        {
            chunkHelper = new();
            GenerateMesh();
        }

        /// <summary>
        /// Uploads the mesh to the gpu
        /// </summary>
        public void Upload()
        {
            vertexBuffer.BufferData();
            IsLoaded = true;
        }

        public bool Render(ID3D11DeviceContext context)
        {
            return vertexBuffer.Render(context);
        }

        public void SetBlockInternal(Block block, int x, int y, int z)
        {
            // Chunk data accessed quickly using bit masks
            data[((x & WorldMap.MASK) + (y & WorldMap.MASK) * CHUNK_SIZE) * (z & WorldMap.MASK) * CHUNK_SIZE_SQUARED] = block;
        }

        public void SetBlock(Block block, Vector3 pos)
        {
            SetBlockInternal(block, (int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        #region Meshing

        private void GenerateMesh()
        {
            // Default 4096, else use the lase size + 1024
            int newSize = vertexBuffer.Used == 0 ? 4096 : vertexBuffer.Used + 1024;
            vertexBuffer.Reset(newSize);

            // Negative X side
            cXN = chunkPosX > 0 ? Map.Chunks[chunkPosX - 1, chunkPosY, chunkPosZ] : null;
            if (cXN is not null && cXN.data is null)
                cXN = null;

            // Positive X side
            cXP = chunkPosX < WorldMap.CHUNK_AMOUNT_X - 1 ? Map.Chunks[chunkPosX + 1, chunkPosY, chunkPosZ] : null;
            if (cXP is not null && cXP.data is null)
                cXP = null;

            // Negative Y side
            cYN = chunkPosY > 0 ? Map.Chunks[chunkPosX, chunkPosY - 1, chunkPosZ] : null;
            if (cYN is not null && cYN.data is null)
                cYN = null;

            // Positive Y side
            cYP = chunkPosY < WorldMap.CHUNK_AMOUNT_Y - 1 ? Map.Chunks[chunkPosX, chunkPosY + 1, chunkPosZ] : null;
            if (cYP is not null && cYP.data is null)
                cYP = null;

            // Negative Z neighbour
            cZN = chunkPosZ > 0 ? Map.Chunks[chunkPosX, chunkPosY, chunkPosZ - 1] : null;
            if (cZN is not null && cZN.data is null)
                cZN = null;

            // Positive Z side
            cZP = chunkPosZ < WorldMap.CHUNK_AMOUNT_Z - 1 ? Map.Chunks[chunkPosX, chunkPosY, chunkPosZ + 1] : null;
            if (cZP is not null && cZP.data is null)
                cZP = null;

            // Precalculate the map-relative Y position of the chunk in the map
            int chunkY = chunkPosY * CHUNK_SIZE;

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
                        ref Block b = ref data[access];

                        if (b.kind != EMPTY)
                        {
                            CreateRun(ref b, i, j, k << 12, i1, k1 << 12, j + chunkY, access, minX, maxX, j == 0, j == CHUNK_SIZE_MINUS_ONE, minZ, maxZ, iCS, kCS2);
                        }
                    }

                    // Extend the array if it is nearly full
                    if (vertexBuffer.Used > vertexBuffer.Data.Length - 2048)
                        vertexBuffer.Extend(2048);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateRun(ref Block b, int i, int j, int k, int i1, int k1, int y, int access, bool minX, bool maxX, bool minY, bool maxY, bool minZ, bool maxZ, int iCS, int kCS2)
        {
            int textureHealth16 = BlockVertex.IndexToTextureShifted[b.index] | b.health / 16 << 23;
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
                        break;

                    chunkHelper.visitXN[chunkAccess++] = true;
                }

                // k1 and k are already shifted
                BlockVertex.AppendQuadX(vertexBuffer, i, jS, length, k1, k, (int)FaceTypeShifted.xn, textureHealth16);
            }

            // Right (X+)
            if (!chunkHelper.visitXP[access] && DrawFaceXP(j, access, maxX, kCS2))
            {
                chunkHelper.visitXP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                        break;

                    chunkHelper.visitXP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadX(vertexBuffer, i1, jS, length, k, k1, (int)FaceTypeShifted.xp, textureHealth16);
            }

            // Back (Z-)
            if (!chunkHelper.visitZN[access] && DrawFaceZN(j, access, minZ, iCS))
            {
                chunkHelper.visitZN[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                        break;

                    chunkHelper.visitZN[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i1, i, jS, length, k, (int)FaceTypeShifted.zn, textureHealth16);
            }

            // Front (Z+)
            if (!chunkHelper.visitZP[access] && DrawFaceZP(j, access, maxZ, iCS))
            {
                chunkHelper.visitZP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                        break;

                    chunkHelper.visitZP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i, i1, jS, length, k1, (int)FaceTypeShifted.zp, textureHealth16);
            }

            // Bottom (Y-)
            if (!chunkHelper.visitYN[access] && DrawFaceYN(access, minY, iCS, kCS2))
            {
                chunkHelper.visitYN[access] = true;

                chunkAccess = access + CHUNK_SIZE;

                for (length = i1; length < CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                        break;

                    chunkHelper.visitYN[chunkAccess] = true;

                    chunkAccess += CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, i, length, jS, k1, k, (int)FaceTypeShifted.yn, textureHealth16);
            }

            // Top (Y+)
            if (!chunkHelper.visitYP[access] && DrawFaceYP(access, maxY, iCS, kCS2))
            {
                chunkHelper.visitYP[access] = true;

                chunkAccess = access + CHUNK_SIZE;

                for (length = i1; length < CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                        break;

                    chunkHelper.visitYP[chunkAccess] = true;

                    chunkAccess += CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, i, length, jS1, k, k1, (int)FaceTypeShifted.yp, textureHealth16);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceXN(int j, int access, bool min, int kCS2)
        {
            if (min)
            {
                if (chunkPosX == 0)
                    return true;

                if (cXN == null)
                    return true;

                // If it is outside this chunk, get the block from the neighbouring chunk
                return cXN.data[31 * CHUNK_SIZE + j + kCS2].index == 0;
            }

            return data[access - CHUNK_SIZE].index == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceXP(int j, int access, bool max, int kCS2)
        {
            if (max)
            {
                if (chunkPosX == WorldMap.CHUNK_AMOUNT_X - 1)
                    return true;

                if (cXP == null)
                    return true;

                // If it is outside this chunk, get the block from the neighbouring chunk
                return cXP.data[j + kCS2].index == 0;
            }

            return data[access + CHUNK_SIZE].index == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceYN(int access, bool min, int iCS, int kCS2)
        {
            if (min)
            {
                if (chunkPosY == 0)
                    return true;

                if (cYN == null)
                    return true;

                // If it is outside this chunk, get the block from the neighbouring chunk
                return cYN.data[iCS + 31 + kCS2].index == 0;
            }

            return data[access - 1].index == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceYP(int access, bool max, int iCS, int kCS2)
        {
            if (max)
            {
                // Don't check chunkYPos here as players can move above the map

                if (cYP == null)
                    return true;

                return cYP.data[iCS + kCS2].index == 0;
            }

            return data[access + 1].index == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceZN(int j, int access, bool min, int iCS)
        {
            if (min)
            {
                if (chunkPosZ == 0)
                    return true;

                if (cZN == null)
                    return true;

                return cZN.data[iCS + j + 31 * CHUNK_SIZE_SQUARED].index == 0;
            }

            return data[access - CHUNK_SIZE_SQUARED].index == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceZP(int j, int access, bool max, int iCS)
        {
            if (max)
            {
                if (chunkPosZ == WorldMap.CHUNK_AMOUNT_Z - 1)
                    return true;

                if (cZP == null)
                    return true;

                return cZP.data[iCS + j].index == 0;
            }

            return data[access + CHUNK_SIZE_SQUARED].index == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DifferentBlock(int chunkAccess, ref Block compare)
        {
            ref var b = ref data[chunkAccess];
            return b.index != compare.index || b.health != compare.health;
        }

        #endregion Meshing

        #region Serialization

        [StructLayout(LayoutKind.Sequential)]
        private struct ChunkRecord
        {
            public byte Health;
            public int Type;
            public Vector3 Position;
        }

        public delegate void SAction(ref Span<byte> s);

        public void SerializeTo(Stream stream)
        {
            ConcurrentQueue<ChunkRecord> savequeue = new();

            int access, heightMapAccess, iCS, kCS2, i1, k1, j, topJ;
            k1 = 1;
            for (int k = 0; k < CHUNK_SIZE; k++, k1++)
            {
                // Calculate this once, rather than multiple times in the inner loop
                kCS2 = k * CHUNK_SIZE_SQUARED;

                i1 = 1;
                heightMapAccess = k * CHUNK_SIZE;

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

                    // X and Z runs search upwards to create runs, so start at the bottom.
                    for (; j < topJ; j++, access++)
                    {
                        ref Block b = ref data[access];

                        if (b.kind != EMPTY)
                        {
                            savequeue.Enqueue(new ChunkRecord() { Position = new(i, j, k), Type = b.index, Health = b.health });
                        }
                    }
                }
            }

            var size = 4 + MinY.Length + MaxY.Length + savequeue.Count * Marshal.SizeOf<ChunkRecord>();
            var result = ArrayPool<byte>.Shared.Rent(size);
            var span = result.AsSpan(0, size);
            var index = 4;
            MinY.CopyTo(span[index..]);
            index += MinY.Length;
            MaxY.CopyTo(span[index..]);
            index += MaxY.Length;
            var buffer = new byte[Marshal.SizeOf<ChunkRecord>()];
            BinaryPrimitives.WriteInt32LittleEndian(span, savequeue.Count);
            while (savequeue.TryDequeue(out var run))
            {
                run.GetBytes(buffer).CopyTo(span[index..]);
                index += buffer.Length;
            }

            data = null;
            MinY = null;
            MaxY = null;
            chunkHelper = null;
            stream.Write(span);
            ArrayPool<byte>.Shared.Return(result);
        }

        public int DeserializeFrom(Span<byte> span)
        {
            if (data is null)
            {
                chunkHelper = new();
                data = new Block[CHUNK_SIZE_CUBED];
                MinY = new byte[CHUNK_SIZE_SQUARED];
                MaxY = new byte[CHUNK_SIZE_SQUARED];
                Array.Fill(MinY, (byte)CHUNK_SIZE);
            }
            var index = 0;
            var count = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
            index += 4;
            span.Slice(index, CHUNK_SIZE_SQUARED).CopyTo(MinY);
            index += CHUNK_SIZE_SQUARED;
            span.Slice(index, CHUNK_SIZE_SQUARED).CopyTo(MaxY);
            index += CHUNK_SIZE_SQUARED;

            var buffer = new byte[Marshal.SizeOf<ChunkRecord>()];
            for (int i = 0; i < count; i++)
            {
                span.Slice(index, buffer.Length).CopyTo(buffer);
                index += buffer.Length;
                var record = buffer.FromBytes<ChunkRecord>();
                data[record.Position.MapToIndex(CHUNK_SIZE, CHUNK_SIZE)] = new Block() { health = record.Health, index = record.Type, kind = 1 };
            }
            return index;
        }

        #endregion Serialization
    }
}