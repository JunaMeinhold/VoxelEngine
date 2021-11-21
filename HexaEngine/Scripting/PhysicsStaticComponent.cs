namespace HexaEngine.Scripting
{
    using BepuPhysics;
    using System.Numerics;

    public class PhysicsStaticComponent : IComponent
    {
        private HexaElement element;

        public StaticDescription StaticDescription { get; set; }

        public StaticHandle Handle { get; private set; }

        public StaticReference Static { get; private set; }

        public virtual void Initialize(HexaElement element)
        {
            this.element = element;
            Handle = this.element.Scene.Simulation.Statics.Add(StaticDescription);
            Static = this.element.Scene.Simulation.Statics.GetStaticReference(Handle);
        }

        public virtual void Uninitialize()
        {
            element.Scene.Simulation.Statics.Remove(Handle);
        }

        public virtual void Update()
        {
            Static = element.Scene.Simulation.Statics.GetStaticReference(Handle);
            element.Transform = Matrix4x4.CreateTranslation(Static.Pose.Position) * Matrix4x4.CreateFromQuaternion(Static.Pose.Orientation);
        }
    }
}