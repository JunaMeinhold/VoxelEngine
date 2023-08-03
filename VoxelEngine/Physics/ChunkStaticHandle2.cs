namespace VoxelEngine.Physics
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using BepuUtilities.Collections;
    using BepuUtilities.Memory;
    using VoxelEngine.Physics.Collidables;
    using VoxelEngine.Voxel;

    public struct ChunkStaticHandle2
    {
        public StaticHandle Handle;
        public Voxels Voxels;
        public TypedIndex Shape;
        public bool IsEmpty;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ChunkStaticHandle2(Simulation simulation, BufferPool pool, Chunk chunk)
        {
            QuickList<Vector3> list = new(Chunk.CHUNK_SIZE_CUBED, pool);
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
                            list.AllocateUnsafely() = new Vector3(i, j, k) + globalOffset;
                        }
                    }
                }
            }

            if (list.Count == 0)
            {
                list.Dispose(pool);
                Handle = default;
                Voxels = default;
                Shape = default;
                IsEmpty = true;
                return;
            }

            Voxels = new Voxels(list, new Vector3(1, 1, 1), pool);
            lock (simulation)
            {
                Shape = simulation.Shapes.Add(Voxels);
                Handle = simulation.Statics.Add(new StaticDescription(chunk.Position, Shape));
            }
        }

        public void Free(Simulation simulation, BufferPool pool)
        {
            if (IsEmpty | simulation == null)
            {
                return;
            }
            lock (simulation)
            {
                simulation.Statics.Remove(Handle);
                Voxels.Dispose(pool);
                simulation.Shapes.Remove(Shape);
            }
        }
    }
}