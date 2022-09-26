namespace VoxelEngine.Physics
{
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using VoxelEngine.Scenes;

    public interface IStaticBodyComponent : IComponent
    {
        ContinuousDetection ContinuousDetection { get; }

        StaticHandle Handle { get; }

        RigidPose Pose { get; set; }

        TypedIndex TypedIndex { get; }
    }

    public interface IStaticBodyComponent<T> : IStaticBodyComponent where T : unmanaged, IShape
    {
        T Shape { get; }
    }
}