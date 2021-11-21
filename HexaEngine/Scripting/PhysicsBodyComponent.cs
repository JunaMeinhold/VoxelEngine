namespace HexaEngine.Scripting
{
    using BepuPhysics;
    using System.Numerics;

    public class PhysicsBodyComponent : IComponent
    {
        private HexaElement element;

        public BodyDescription BodyDescription { get; set; }

        public BodyHandle Handle { get; private set; }

        public BodyReference Body { get; private set; }

        public virtual void Initialize(HexaElement element)
        {
            this.element = element;
            Handle = this.element.Scene.Simulation.Bodies.Add(BodyDescription);
            Body = this.element.Scene.Simulation.Bodies.GetBodyReference(Handle);
        }

        public virtual void Uninitialize()
        {
            element.Scene.Simulation.Bodies.Remove(Handle);
        }

        public virtual void Update()
        {
            Body = element.Scene.Simulation.Bodies.GetBodyReference(Handle);
            element.Transform = Matrix4x4.CreateTranslation(Body.Pose.Position) * Matrix4x4.CreateFromQuaternion(Body.Pose.Orientation);
        }
    }
}