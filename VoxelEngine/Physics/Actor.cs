namespace HexaEngine.Physics
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Vortice.Mathematics;

    public class Actor
    {
        private const float DegToRadFactor = 0.0174532925f;

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector3 Accelleration { get; set; }

        public Vector3 Velocity { get; set; }

        public Vector3 AngularAccelleration { get; set; }

        public Vector3 AngularVelocity { get; set; }

        public Vector3 Force { get; set; }

        public float Mass { get; set; }

        public ActorType Type { get; set; }

        public List<Actor> CollisionList { get; } = new();

        public event EventHandler<CollisionEventArgs> OnCollision;

        public event EventHandler<CollisionEventArgs> OnContactLoss;

        public BoundingBox BoundingBox { get; set; }

        public Matrix4x4 Transform { get; set; }

        internal void UpdateBody(float dt)
        {
            Velocity += Accelleration * dt;
            Position += Velocity * dt;
            AngularVelocity += AngularAccelleration * dt;
            Rotation += AngularVelocity * dt;
            Force = Vector3.Zero;
            Transform = Matrix4x4.CreateFromYawPitchRoll(Rotation.Y * DegToRadFactor, Rotation.X * DegToRadFactor, Rotation.Z * DegToRadFactor) * Matrix4x4.CreateTranslation(Position);
        }

        public void AddCollision(Actor actor)
        {
            CollisionList.Add(actor);
            OnCollision?.Invoke(this, new() { Collider = actor });
        }

        public void RemoveCollision(Actor actor)
        {
            CollisionList.Remove(actor);
            OnContactLoss?.Invoke(this, new() { Collider = actor });
        }

        public void ApplyForce(Vector3 force)
        {
            Force += force;
        }
    }

    public class CollisionEventArgs : EventArgs
    {
        public Actor Collider { get; init; }
    }
}