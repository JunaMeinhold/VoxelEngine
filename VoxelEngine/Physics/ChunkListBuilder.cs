namespace VoxelEngine.Physics
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using BepuUtilities.Collections;
    using BepuUtilities.Memory;
    using VoxelEngine.Voxel;

    public static class ChunkListBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuickList<Vector3> Build(BufferPool bufferPool, Chunk chunk)
        {
            QuickList<Vector3> result = new(Chunk.CHUNK_SIZE_CUBED, bufferPool);
            Vector3 globalOffset = chunk.Position * Chunk.CHUNK_SIZE;
            for (int k = 0; k < Chunk.CHUNK_SIZE; k++)
            {
                // Calculate this once, rather than multiple times in the inner loop
                int kCS2 = k * Chunk.CHUNK_SIZE_SQUARED;

                int heightMapAccess = k * Chunk.CHUNK_SIZE;

                for (int i = 0; i < Chunk.CHUNK_SIZE; i++)
                {
                    // Determine where to start the innermost loop
                    int j = chunk.MinY[heightMapAccess];
                    int topJ = chunk.MaxY[heightMapAccess];
                    heightMapAccess++;

                    // Calculate this once, rather than multiple times in the inner loop
                    int iCS = i * Chunk.CHUNK_SIZE;

                    // Calculate access here and increment it each time in the innermost loop
                    int access = kCS2 + iCS + j;

                    // X and Z runs search upwards to create runs, so start at the bottom.
                    for (; j < topJ; j++, access++)
                    {
                        ref Block b = ref chunk.Data[access];

                        if (b.Type != Chunk.EMPTY)
                        {
                            result.AllocateUnsafely() = new Vector3(i, j, k) + globalOffset;
                        }
                    }
                }
            }

            return result;
        }
    }
}