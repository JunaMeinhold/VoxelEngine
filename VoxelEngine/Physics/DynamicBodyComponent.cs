namespace VoxelEngine.Physics
{
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using Hexa.NET.D3D11;
    using VoxelEngine.Scenes;

    public class DynamicBodyComponent<T> : IBodyComponent, IDynamicBodyComponent<T> where T : unmanaged, IShape
    {
        private readonly T shape;
        private BodyInertia inertia;
        private RigidPose pose;
        private TypedIndex typedIndex;
        private BodyHandle handle;

        private GameObject sceneElement;

        public DynamicBodyComponent(T shape, BodyInertia inertia, RigidPose pose)
        {
            this.shape = shape;
            this.inertia = inertia;
            this.pose = pose;
        }

        public BodyHandle Handle => handle;

        public T Shape => shape;

        public BodyInertia Inertia => inertia;

        public TypedIndex TypedIndex => typedIndex;

        public RigidPose Pose => pose;

        public void Initialize(GameObject element)
        {
            sceneElement = element;
            typedIndex = sceneElement.Scene.Simulation.Shapes.Add(shape);
            handle = sceneElement.Scene.Simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, new CollidableDescription(typedIndex, 0), new BodyActivityDescription(0.01f)));
        }

        public void Uninitialize()
        {
        }

        public void Update()
        {
            BodyReference reference = sceneElement.Scene.Simulation.Bodies.GetBodyReference(handle);
            pose = reference.Pose;
            sceneElement.Transform.Position = pose.Position;
            sceneElement.Transform.Orientation = pose.Orientation;
        }
    }
}