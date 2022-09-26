namespace VoxelEngine.Scenes
{
    using System.Numerics;
    using VoxelEngine.Audio;
    using VoxelEngine.Scripting;

    public class EmitterComponent : ScriptFrameComponent
    {
        public SoundEmitter Emitter { get; private set; }

        public override void Awake()
        {
            Emitter = new();
        }

        public override void Destroy()
        {
            Emitter.Dispose();
        }

        public override void Update()
        {
            Emitter.Position = Parent.Transform.Position;
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(Parent.Transform.Orientation);
            Emitter.Velocity = Vector3.Zero;
            Emitter.OrientTop = Vector3.Transform(Vector3.UnitY, rot);
            Emitter.OrientFront = Vector3.Transform(Vector3.UnitZ, rot);
            Emitter.Update();
        }
    }
}