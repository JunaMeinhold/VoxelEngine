namespace VoxelEngine.Voxel
{
    using BepuPhysics;

    public class Entitiy
    {
        public RigidPose Pose { get; set; }

        public BodyHandle Body { get; set; }
    }
}