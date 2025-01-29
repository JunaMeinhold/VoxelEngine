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

        public GameObject GameObject { get; set; } = null!;

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

        public void Awake()
        {
            typedIndex = GameObject.Scene.Simulation.Shapes.Add(shape);
            handle = GameObject.Scene.Simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, new CollidableDescription(typedIndex, 0), new BodyActivityDescription(0.01f)));
        }

        public void Destroy()
        {
        }

        public void Update()
        {
            BodyReference reference = GameObject.Scene.Simulation.Bodies.GetBodyReference(handle);
            pose = reference.Pose;
            GameObject.Transform.Position = pose.Position;
            GameObject.Transform.Orientation = pose.Orientation;
        }
    }
}