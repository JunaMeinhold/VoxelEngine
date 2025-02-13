namespace App.Scripts
{
    using System.Numerics;
    using VoxelEngine.Physics;
    using VoxelEngine.Scenes;

    public unsafe class DynamicActorComponent : IPhysicsComponent
    {
        private DynamicActor* actor;

        public GameObject GameObject { get; set; } = null!;

        public bool IsGrounded => actor->Grounded;

        public void SetPosition(Vector3 position)
        {
            actor->SetPosition(position);
        }

        public void Move(Vector3 position)
        {
            actor->Move(position);
        }

        public void Awake()
        {
            actor = GameObject.Scene.Physics.CreateActor();
            actor->AddShape(new BoxShape(new(0.5f, 2, 0.5f)));
        }

        public void Destroy()
        {
            GameObject.Scene.Physics.DestroyActor(actor);
        }

        public void PreTick(PhysicsSystem system)
        {
            actor->SetPosition(GameObject.Transform.GlobalPosition);
        }

        public void PostTick(PhysicsSystem system)
        {
            GameObject.Transform.GlobalPosition = actor->GetPosition();
        }
    }
}