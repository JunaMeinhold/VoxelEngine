namespace VoxelEngine.Voxel.Meshing
{
    using System.Runtime.CompilerServices;
    using VoxelEngine.Voxel;

    public class VoxelMeshFactory
    {
        private ChunkHelper chunkHelper;
        private Chunk chunk;
        private IVoxelVertexBuffer vertexBuffer;

        public void GenerateMesh(IVoxelVertexBuffer vertexBuffer, Chunk chunk)
        {
            this.vertexBuffer = vertexBuffer;
            this.chunk = chunk;
            chunkHelper = new();

            // Default 4096, else use the lase size + 1024
            int newSize = vertexBuffer.Count == 0 ? 4096 : vertexBuffer.Count + 1024;
            vertexBuffer.Reset(newSize);

            // Negative X side
            chunk.cXN = chunk.Map.Chunks[(int)(chunk.Position.X - 1), (int)chunk.Position.Y, (int)chunk.Position.Z];
            if (chunk.cXN is not null && chunk.cXN.Data is null)
            {
                chunk.cXN = null;
            }

            // Positive X side
            chunk.cXP = chunk.Map.Chunks[(int)(chunk.Position.X + 1), (int)chunk.Position.Y, (int)chunk.Position.Z];
            if (chunk.cXP is not null && chunk.cXP.Data is null)
            {
                chunk.cXP = null;
            }

            // Negative Y side
            chunk.cYN = chunk.Position.Y > 0 ? chunk.Map.Chunks[(int)chunk.Position.X, (int)(chunk.Position.Y - 1), (int)chunk.Position.Z] : null;
            if (chunk.cYN is not null && chunk.cYN.Data is null)
            {
                chunk.cYN = null;
            }

            // Positive Y side
            chunk.cYP = chunk.Position.Y < WorldMap.CHUNK_AMOUNT_Y - 1 ? chunk.Map.Chunks[(int)chunk.Position.X, (int)(chunk.Position.Y + 1), (int)chunk.Position.Z] : null;
            if (chunk.cYP is not null && chunk.cYP.Data is null)
            {
                chunk.cYP = null;
            }

            // Negative Z neighbour
            chunk.cZN = chunk.Map.Chunks[(int)chunk.Position.X, (int)chunk.Position.Y, (int)(chunk.Position.Z - 1)];
            if (chunk.cZN is not null && chunk.cZN.Data is null)
            {
                chunk.cZN = null;
            }

            // Positive Z side
            chunk.cZP = chunk.Map.Chunks[(int)chunk.Position.X, (int)chunk.Position.Y, (int)(chunk.Position.Z + 1)];
            if (chunk.cZP is not null && chunk.cZP.Data is null)
            {
                chunk.cZP = null;
            }

            chunk.MissingNeighbours = chunk.cXN == null || chunk.cXP == null || chunk.cYN == null || chunk.cYP == null || chunk.cZN == null || chunk.cZP == null;

            // Precalculate the map-relative Y position of the chunk in the map
            int chunkY = (int)(chunk.Position.Y * Chunk.CHUNK_SIZE);

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

                    // X and Z runs search upwards to create runs, so start at the bottom.
                    for (; j < topJ; j++, access++)
                    {
                        ref Block b = ref chunk.Data[access];

                        if (b.Type != Chunk.EMPTY)
                        {
                            CreateRun(ref b, i, j, k << 12, i1, k1 << 12, j + chunkY, access, minX, maxX, j == 0, j == Chunk.CHUNK_SIZE_MINUS_ONE, minZ, maxZ, iCS, kCS2);
                        }
                    }

                    // Extend the array if it is nearly full
                    if (vertexBuffer.Count > vertexBuffer.Capacity - 2048)
                    {
                        vertexBuffer.EnsureCapacity(vertexBuffer.Capacity + 2048);
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

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitXN[chunkAccess++] = true;
                }

                // k1 and k are already shifted
                BlockVertex.AppendQuadX(vertexBuffer, i, jS, length, k1, k, (int)FaceTypeShifted.XN, textureHealth16);
            }

            // Right (X+)
            if (!chunkHelper.visitXP[access] && DrawFaceXP(j, access, maxX, kCS2))
            {
                chunkHelper.visitXP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitXP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadX(vertexBuffer, i1, jS, length, k, k1, (int)FaceTypeShifted.XP, textureHealth16);
            }

            // Back (Z-)
            if (!chunkHelper.visitZN[access] && DrawFaceZN(j, access, minZ, iCS))
            {
                chunkHelper.visitZN[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitZN[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i1, i, jS, length, k, (int)FaceTypeShifted.ZN, textureHealth16);
            }

            // Front (Z+)
            if (!chunkHelper.visitZP[access] && DrawFaceZP(j, access, maxZ, iCS))
            {
                chunkHelper.visitZP[access] = true;

                chunkAccess = accessIncremented;

                for (length = jS1; length < Chunk.CHUNK_SIZE_SHIFTED; length += 1 << 6)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitZP[chunkAccess++] = true;
                }

                BlockVertex.AppendQuadZ(vertexBuffer, i, i1, jS, length, k1, (int)FaceTypeShifted.ZP, textureHealth16);
            }

            // Bottom (Y-)
            if (!chunkHelper.visitYN[access] && DrawFaceYN(access, minY, iCS, kCS2))
            {
                chunkHelper.visitYN[access] = true;

                chunkAccess = access + Chunk.CHUNK_SIZE;

                for (length = i1; length < Chunk.CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitYN[chunkAccess] = true;

                    chunkAccess += Chunk.CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, i, length, jS, k1, k, (int)FaceTypeShifted.YN, textureHealth16);
            }

            // Top (Y+)
            if (!chunkHelper.visitYP[access] && DrawFaceYP(access, maxY, iCS, kCS2))
            {
                chunkHelper.visitYP[access] = true;

                chunkAccess = access + Chunk.CHUNK_SIZE;

                for (length = i1; length < Chunk.CHUNK_SIZE; length++)
                {
                    if (DifferentBlock(chunkAccess, ref b))
                    {
                        break;
                    }

                    chunkHelper.visitYP[chunkAccess] = true;

                    chunkAccess += Chunk.CHUNK_SIZE;
                }

                BlockVertex.AppendQuadY(vertexBuffer, i, length, jS1, k, k1, (int)FaceTypeShifted.YP, textureHealth16);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceXN(int j, int access, bool min, int kCS2)
        {
            if (min)
            {
                if (chunk.cXN == null)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return chunk.cXN.Data[Chunk.CHUNK_SIZE_MINUS_ONE * Chunk.CHUNK_SIZE + j + kCS2].Type == 0;
            }

            return chunk.Data[access - Chunk.CHUNK_SIZE].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceXP(int j, int access, bool max, int kCS2)
        {
            if (max)
            {
                if (chunk.cXP == null)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return chunk.cXP.Data[j + kCS2].Type == 0;
            }

            return chunk.Data[access + Chunk.CHUNK_SIZE].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceYN(int access, bool min, int iCS, int kCS2)
        {
            if (min)
            {
                if (chunk.Position.Y == 0)
                {
                    return true;
                }

                if (chunk.cYN == null)
                {
                    return true;
                }

                // If it is outside this chunk, get the block from the neighbouring chunk
                return chunk.cYN.Data[iCS + Chunk.CHUNK_SIZE_MINUS_ONE + kCS2].Type == 0;
            }

            return chunk.Data[access - 1].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceYP(int access, bool max, int iCS, int kCS2)
        {
            if (max)
            {
                // Don't check chunkYPos here as players can move above the map
                if (chunk.cYP == null)
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
        protected bool DrawFaceZN(int j, int access, bool min, int iCS)
        {
            if (min)
            {
                if (chunk.cZN == null)
                {
                    return true;
                }

                return chunk.cZN.Data[iCS + j + Chunk.CHUNK_SIZE_MINUS_ONE * Chunk.CHUNK_SIZE_SQUARED].Type == 0;
            }

            return chunk.Data[access - Chunk.CHUNK_SIZE_SQUARED].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DrawFaceZP(int j, int access, bool max, int iCS)
        {
            if (max)
            {
                if (chunk.cZP == null)
                {
                    return true;
                }

                return chunk.cZP.Data[iCS + j].Type == 0;
            }

            return chunk.Data[access + Chunk.CHUNK_SIZE_SQUARED].Type == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool DifferentBlock(int chunkAccess, ref Block compare)
        {
            ref Block b = ref chunk.Data[chunkAccess];
            return b.Type != compare.Type;
        }
    }
}