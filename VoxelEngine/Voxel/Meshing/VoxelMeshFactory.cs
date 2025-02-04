namespace VoxelEngine.Voxel.Meshing
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Voxel.Blocks;

    public static unsafe class VoxelMeshFactory
    {
        public static void GenerateMesh(ChunkVertexBuffer* vertexBuffer, Chunk chunk)
        {
            vertexBuffer->Lock();
            // Default 4096, else use the lase size + 1024
            int newSize = vertexBuffer->Count == 0 ? 4096 : vertexBuffer->Count;
            vertexBuffer->Reset(newSize);

            Vector3 position = chunk.Position;

            // Negative X side
            chunk.cXN = chunk.Map.Chunks[(int)(position.X - 1), (int)position.Y, (int)position.Z];
            if (chunk.cXN is not null)
            {
                if (!chunk.cXN.InMemory)
                {
                    chunk.cXN = null;
                }
            }

            // Positive X side
            chunk.cXP = chunk.Map.Chunks[(int)(position.X + 1), (int)position.Y, (int)position.Z];
            if (chunk.cXP is not null)
            {
                if (!chunk.cXP.InMemory)
                {
                    chunk.cXP = null;
                }
            }

            // Negative Y side
            chunk.cYN = chunk.Position.Y > 0 ? chunk.Map.Chunks[(int)position.X, (int)(position.Y - 1), (int)position.Z] : null;
            if (chunk.cYN is not null)
            {
                if (!chunk.cYN.InMemory)
                {
                    chunk.cYN = null;
                }
            }

            // Positive Y side
            chunk.cYP = chunk.Position.Y < World.CHUNK_AMOUNT_Y - 1 ? chunk.Map.Chunks[(int)position.X, (int)(position.Y + 1), (int)position.Z] : null;
            if (chunk.cYP is not null)
            {
                if (!chunk.cYP.InMemory)
                {
                    chunk.cYP = null;
                }
            }

            // Negative Z neighbour
            chunk.cZN = chunk.Map.Chunks[(int)position.X, (int)position.Y, (int)(position.Z - 1)];
            if (chunk.cZN is not null)
            {
                if (!chunk.cZN.InMemory)
                {
                    chunk.cZN = null;
                }
            }

            // Positive Z side
            chunk.cZP = chunk.Map.Chunks[(int)position.X, (int)position.Y, (int)(position.Z + 1)];
            if (chunk.cZP is not null)
            {
                if (!chunk.cZP.InMemory)
                {
                    chunk.cZP = null;
                }
            }

            chunk.HasMissingNeighbours = chunk.cXN == null || chunk.cXP == null || chunk.cYN == null || chunk.cYP == null || chunk.cZN == null || chunk.cZP == null;

            // Precalculate the map-relative Y position of the chunk in the map
            int chunkY = (int)(position.Y * Chunk.CHUNK_SIZE);

            // Allocate variables on the stack
            int access, heightMapAccess, iCS, kCS2, i1, k1, j, topJ;
            bool minX, maxX, minZ, maxZ;

            k1 = 1;

            for (int k = 0; k < Chunk.CHUNK_SIZE; k++, k1++)
            {
                // Calculate this once, rather than multiple times in the inner loop
                kCS2 = k * Chunk.CHUNK_SIZE_SQUARED;

                i1 = 1;
                heightMapAccess = k * Chunk.CHUNK_SIZE;

                // Is the current run on the Z- or Z+ edge of the chunk
                minZ = k == 0;
                maxZ = k == Chunk.CHUNK_SIZE_MINUS_ONE;

                for (int i = 0; i < Chunk.CHUNK_SIZE; i++, i1++)
                {
                    // Determine where to start the innermost loop
                    j = chunk.MinY[heightMapAccess];
                    topJ = chunk.MaxY[heightMapAccess];
                    heightMapAccess++;

                    // Calculate this once, rather than multiple times in the inner loop
                    iCS = i * Chunk.CHUNK_SIZE;

                    // Calculate access here and increment it each time in the innermost loop
                    access = kCS2 + iCS + j;

                    // Is the current run on the X- or X+ edge of the chunk
                    minX = i == 0;
                    maxX = i == Chunk.CHUNK_SIZE_MINUS_ONE;

                    // X and Z runs search upwards to create runs, so start at the botto
                    for (; j < topJ; j++, access++)
                    {
                        Block* b = chunk.Data.Data + access;

                        if (b->Type != Chunk.EMPTY)
                        {
                            CreateRun(vertexBuffer, chunk, b, i, j, k << 12, i1, k1 << 12, j + chunkY, access, minX, maxX, j == 0, j == Chunk.CHUNK_SIZE_MINUS_ONE, minZ, maxZ, iCS, kCS2);
                        }
                    }
                }
            }

            vertexBuffer->ReleaseLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CreateRun(ChunkVertexBuffer* vertexBuffer, Chunk chunk, Block* b, int i, int j, int k, int i1, int k1, int y, int access, bool minX, bool maxX, bool minY, bool maxY, bool minZ, bool maxZ, int iCS, int kCS2)
        {
            Block* data = chunk.Data.Data;
            int type = b->Type;
            ChunkHelper chunkHelper = chunk.ChunkHelper;
            int textureHealth16 = BlockVertex.IndexToTextureShifted[type];
            int accessIncremented = access + 1;
            int chunkAccess;
            int j1 = j + 1;
            int jS = j << 6;
            int jS1 = j1 << 6;
            int length;
            uint tint = GetTint(type, access);

            // Left (X-)
            if (!chunkHelper.visitXN[access] && DrawFaceXN(chunk, j, access, minX, kCS2))
            {
                chunkHelper.visitXN[access] = true;
                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(data + chunkAccess, type))
                    {
                        break;
                    }

                    chunkHelper.visitXN[chunkAccess++] = true;
                }

                // k1 and k are already shifted
                BlockVertex.AppendQuadX(vertexBuffer, i, jS, length, k1, k, (int)FaceTypeShifted.XN, textureHealth16, tint);
            }

            // Right (X+)
            if (!chunkHelper.visitXP[access] && DrawFaceXP(chunk, j, access, maxX, kCS2))
            {
                chunkHelper.visitXP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(data + chunkAccess, type))
                    {
                        break;
                    }

                    chunkHelper.visitXP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadX(vertexBuffer, i1, jS, length, k, k1, (int)FaceTypeShifted.XP, textureHealth16, tint);
            }

            // Back (Z-)
            if (!chunkHelper.visitZN[access] && DrawFaceZN(chunk, j, access, minZ, iCS))
            {
                chunkHelper.visitZN[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(data + chunkAccess, type))
                    {
                        break;
                    }

                    chunkHelper.visitZN[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i1, i, jS, length, k, (int)FaceTypeShifted.ZN, textureHealth16, tint);
            }

            // Front (Z+)
            if (!chunkHelper.visitZP[access] && DrawFaceZP(chunk, j, access, maxZ, iCS))
            {
                chunkHelper.visitZP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(data + chunkAccess, type))
                    {
                        break;
                    }

                    chunkHelper.visitZP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i, i1, jS, length, k1, (int)FaceTypeShifted.ZP, textureHealth16, tint);
            }

            // Bottom (Y-)
            if (!chunkHelper.visitYN[access] && DrawFaceYN(chunk, access, minY, iCS, kCS2))
            {
                chunkHelper.visitYN[access] = true;

                chunkAccess = access + Chunk.CHUNK_SIZE;

                for (length = i1; length < Chunk.CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(data + chunkAccess, type))
                    {
                        break;
                    }

                    chunkHelper.visitYN[chunkAccess] = true;

                    chunkAccess += Chunk.CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, i, length, jS, k1, k, (int)FaceTypeShifted.YN, textureHealth16, tint);
            }

            // Top (Y+)
            if (!chunkHelper.visitYP[access] && DrawFaceYP(chunk, access, maxY, iCS, kCS2))
            {
                chunkHelper.visitYP[access] = true;

                chunkAccess = access + Chunk.CHUNK_SIZE;

                for (length = i1; length < Chunk.CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(data + chunkAccess, type))
                    {
                        break;
                    }

                    chunkHelper.visitYP[chunkAccess] = true;

                    chunkAccess += Chunk.CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, i, length, jS1, k, k1, (int)FaceTypeShifted.YP, textureHealth16, tint);
            }
        }

        private static uint GetTint(int type, int access)
        {
            if (type == 10)
            {
                return 0xFF619961; // ABGR
            }

            return uint.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceXN(Chunk chunk, int j, int access, bool min, int kCS2)
        {
            if (min)
            {
                if (chunk.cXN == null || !chunk.cXN.InMemory)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return chunk.cXN.Data[Chunk.CHUNK_SIZE_MINUS_ONE * Chunk.CHUNK_SIZE + j + kCS2].Type == 0;
            }

            return chunk.Data[access - Chunk.CHUNK_SIZE].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceXP(Chunk chunk, int j, int access, bool max, int kCS2)
        {
            if (max)
            {
                if (chunk.cXP == null || !chunk.cXP.InMemory)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return chunk.cXP.Data[j + kCS2].Type == 0;
            }

            return chunk.Data[access + Chunk.CHUNK_SIZE].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceYN(Chunk chunk, int access, bool min, int iCS, int kCS2)
        {
            if (min)
            {
                if (chunk.Position.Y == 0)
                {
                    return true;
                }

                if (chunk.cYN == null || !chunk.cYN.InMemory)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return chunk.cYN.Data[iCS + Chunk.CHUNK_SIZE_MINUS_ONE + kCS2].Type == 0;
            }

            return chunk.Data[access - 1].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceYP(Chunk chunk, int access, bool max, int iCS, int kCS2)
        {
            if (max)
            {
                // Don't check chunkYPos here as players can move above the map
                if (chunk.cYP == null || !chunk.cYP.InMemory)
                {
                    return true;
                }
                else
                {
                    return chunk.cYP.Data[iCS + kCS2].Type == 0;
                }
            }
            else
            {
                return chunk.Data[access + 1].Type == 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceZN(Chunk chunk, int j, int access, bool min, int iCS)
        {
            if (min)
            {
                if (chunk.cZN == null || chunk.cZN.InMemory)
                {
                    return true;
                }

                return chunk.cZN.Data[iCS + j + Chunk.CHUNK_SIZE_MINUS_ONE * Chunk.CHUNK_SIZE_SQUARED].Type == 0;
            }

            return chunk.Data[access - Chunk.CHUNK_SIZE_SQUARED].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DrawFaceZP(Chunk chunk, int j, int access, bool max, int iCS)
        {
            if (max)
            {
                if (chunk.cZP == null || !chunk.cZP.InMemory)
                {
                    return true;
                }

                return chunk.cZP.Data[iCS + j].Type == 0;
            }

            return chunk.Data[access + Chunk.CHUNK_SIZE_SQUARED].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DifferentBlock(Chunk chunk, int chunkAccess, int compare)
        {
            Block b = chunk.Data[chunkAccess];
            if (BlockRegistry.AlphaTest.Contains(b)) return true;
            return b.Type != compare;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DifferentBlock(Block* data, int compare)
        {
            Block b = *data;
            if (BlockRegistry.AlphaTest.Contains(b)) return true;
            return b.Type != compare;
        }
    }
}