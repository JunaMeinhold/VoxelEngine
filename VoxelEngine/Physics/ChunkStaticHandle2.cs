namespace VoxelEngine.Physics
{
    using System.Numerics;
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using BepuUtilities.Collections;
    using BepuUtilities.Memory;
    using VoxelEngine.Physics.Collidables;
    using VoxelEngine.Voxel;

    public class ChunkStaticHandle2
    {
        public Voxels Voxels;
        public TypedIndex Shape;
        public StaticHandle Handle;
        public bool IsEmpty;

        public ChunkStaticHandle2(Simulation simulation, BufferPool pool, Chunk chunk)
        {
            QuickList<Vector3> list = ChunkListBuilder.Build(pool, chunk);
            if (list.Count > 0)
            {
                Voxels = new Voxels(list, new Vector3(1, 1, 1), pool);
                Shape = simulation.Shapes.Add(Voxels);
                Handle = simulation.Statics.Add(new StaticDescription(Vector3.Zero, Shape));
            }
            else
            {
                IsEmpty = true;
                list.Dispose(pool);
            }
        }

        public void Free(Simulation simulation, BufferPool pool)
        {
            if (IsEmpty | simulation == null)
            {
                return;
            }

            simulation.Statics.Remove(Handle);
            simulation.Shapes.Remove(Shape);
            Voxels.Dispose(pool);
        }
    }
}