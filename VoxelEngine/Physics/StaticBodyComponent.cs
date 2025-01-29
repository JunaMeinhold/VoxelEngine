namespace VoxelEngine.Physics
{
    using System.Runtime.CompilerServices;
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using Hexa.NET.D3D11;
    using VoxelEngine.Scenes;

    public class StaticBodyComponent<T> : IBodyComponent, IStaticBodyComponent<T> where T : unmanaged, IShape
    {
        private readonly T shape;
        private RigidPose pose;
        private ContinuousDetection continuousDetection;
        private TypedIndex typedIndex;
        private StaticHandle handle;
        private StaticReference reference;
        private bool overwritePosition;

        private GameObject sceneElement;

        public StaticBodyComponent(T shape, RigidPose pose, ContinuousDetection continuousDetection)
        {
            this.shape = shape;
            this.pose = pose;
            this.continuousDetection = continuousDetection;
        }

        public T Shape => shape;

        public RigidPose Pose
        {
            get => pose;
            set
            {
                pose = value;
                overwritePosition = true;
            }
        }

        public ContinuousDetection ContinuousDetection => continuousDetection;

        public TypedIndex TypedIndex => typedIndex;

        public StaticHandle Handle => handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(GameObject element)
        {
            sceneElement = element;
            typedIndex = sceneElement.Scene.Simulation.Shapes.Add(shape);
            handle = sceneElement.Scene.Simulation.Statics.Add(new StaticDescription(pose, typedIndex, continuousDetection));
            reference = sceneElement.Scene.Simulation.Statics.GetStaticReference(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            sceneElement.Scene.Simulation.Statics.Remove(handle);
            sceneElement.Scene.Simulation.Shapes.Remove(typedIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            if (overwritePosition)
            {
                reference.Pose = pose;
                overwritePosition = false;
            }
            else
            {
                pose = reference.Pose;
            }

            sceneElement.Transform.Position = pose.Position;
            sceneElement.Transform.Orientation = pose.Orientation;
        }
    }
}