namespace VoxelEngine.Voxel
{
    using BepuPhysics;

    public class Entity
    {
        public RigidPose Pose { get; set; }

        public BodyHandle Body { get; set; }
    }
}