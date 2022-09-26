namespace VoxelEngine.Scenes
{
    using System.Numerics;
    using VoxelEngine.Audio;
    using VoxelEngine.Scripting;

    public class ListenerComponent : ScriptFrameComponent
    {
        public SoundListener Listener { get; private set; }

        public bool IsActive { get; set; }

        public override void Awake()
        {
            Listener = new();
            if (IsActive)
            {
                Listener.Activate();
            }
        }

        public override void Destroy()
        {
            Listener?.Dispose();
        }

        public override void Update()
        {
            Listener.Position = Parent.Transform.Position;
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(Parent.Transform.Orientation);
            Listener.Velocity = Vector3.Zero;
            Listener.OrientTop = Vector3.Transform(Vector3.UnitY, rot);
            Listener.OrientFront = Vector3.Transform(Vector3.UnitZ, rot);
        }
    }
}