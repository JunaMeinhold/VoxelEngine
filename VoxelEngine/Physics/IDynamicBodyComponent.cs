namespace VoxelEngine.Physics
{
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using VoxelEngine.Scenes;

    public interface IDynamicBodyComponent : IComponent
    {
        BodyHandle Handle { get; }

        BodyInertia Inertia { get; }

        RigidPose Pose { get; }

        TypedIndex TypedIndex { get; }
    }

    public interface IDynamicBodyComponent<T> : IDynamicBodyComponent where T : unmanaged, IShape
    {
        T Shape { get; }
    }
}