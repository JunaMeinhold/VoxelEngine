namespace VoxelEngine.Physics
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using BepuUtilities.Memory;
    using VoxelEngine.Voxel;

    public struct ChunkStaticHandle
    {
        public StaticHandle Handle;
        public BigCompound Compound;
        public TypedIndex Shape;
        public bool IsEmpty;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ChunkStaticHandle(Simulation simulation, BufferPool pool, Chunk chunk)
        {
            IsEmpty = false;
            Box box = new(1, 1, 1);
            CompoundBuilder compoundBuilder = new(pool, simulation.Shapes, Chunk.CHUNK_SIZE_CUBED);
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
                            Vector3 relativePosition = new(i, j, k);
                            RigidPose pose = new(relativePosition + new Vector3(0.5f, 0.5f, 0.5f));
                            compoundBuilder.Add(box, pose, 10);
                        }
                    }
                }
            }

            if (compoundBuilder.Children.Count == 0)
            {
                compoundBuilder.Dispose();
                Handle = default;
                Compound = default;
                Shape = default;
                IsEmpty = true;
                return;
            }
            compoundBuilder.BuildKinematicCompound(out Buffer<CompoundChild> compoundChildren, out Vector3 offset);
            lock (simulation)
            {
                Compound = new(compoundChildren, simulation.Shapes, pool);
                Shape = simulation.Shapes.Add(Compound);
                Handle = simulation.Statics.Add(new StaticDescription(chunk.Position * Chunk.CHUNK_SIZE + offset, Shape));
            }

            compoundBuilder.Dispose();
        }

        public void Free(Simulation simulation, BufferPool pool)
        {
            if (simulation == null | IsEmpty)
            {
                return;
            }
            lock (simulation)
            {
                simulation.Statics.Remove(Handle);
                Compound.Dispose(pool);
                simulation.Shapes.Remove(Shape);
            }
        }
    }
}