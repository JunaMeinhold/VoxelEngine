namespace VoxelEngine.Physics
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public struct RaycastHit
    {
        public Vector3 Position;
        public Vector3 Normal;
        public bool Hit;
        public int BlockX, BlockY, BlockZ;
    }

    public struct DynamicActor
    {
        public Vector3 Position;
        public Vector3 Rotation;

        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    public unsafe class PhysicsSystem : ISceneSystem
    {
        private World world;
        private UnsafeList<Pointer<DynamicActor>> actors;

        public string Name { get; } = "Physics System";

        public SystemFlags Flags { get; } = SystemFlags.Awake | SystemFlags.Destroy | SystemFlags.PhysicsUpdate;

        public void Awake(Scene scene)
        {
            world = scene.Find<World>()!;
        }

        public void Destroy()
        {
        }

        public DynamicActor* CreateActor()
        {
            var actor = AllocT<DynamicActor>();
            ZeroMemoryT(actor);
            actors.Add(actor);
            return actor;
        }

        public void DestroyActor(DynamicActor* actor)
        {
            actors.Remove(actor);
            Free(actor);
        }

        public void FixedUpdate()
        {
            float deltaTime = Time.FixedDelta;

            for (int i = 0; i < actors.Count; i++)
            {
                DynamicActor* actor = actors[i];

                if (!IsOnGround(actor))
                {
                    actor->LinearVelocity.Y -= 9.81f * deltaTime;
                }

                if (actor->LinearVelocity.Y > 0 && HitCeiling(actor))
                {
                    actor->LinearVelocity.Y = 0;
                }

                actor->Position += actor->LinearVelocity * deltaTime;
            }
        }

        public unsafe bool IsOnGround(DynamicActor* actor, float maxCheckDistance = 1.5f)
        {
            RaycastHit hit = CastRay(actor->Position, -Vector3.UnitY, maxCheckDistance);

            if (hit.Hit)
            {
                actor->Position.Y = hit.Position.Y + 1;
                actor->LinearVelocity.Y = 0;
                return true;
            }

            return false;
        }

        public unsafe bool HitCeiling(DynamicActor* actor, float maxCheckDistance = 0.5f)
        {
            RaycastHit hit = CastRay(actor->Position, Vector3.UnitY, maxCheckDistance);

            if (hit.Hit)
            {
                actor->Position.Y = hit.Position.Y - 1;
                actor->LinearVelocity.Y = 0;
                return true;
            }

            return false;
        }

        public RaycastHit CastRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return CastRay(origin, direction, maxDistance, world);
        }

        public static RaycastHit CastRay(Vector3 origin, Vector3 direction, float maxDistance, World world)
        {
            Vector3D rayPos = origin;
            Vector3D rayDir = Vector3D.Normalize(direction);

            long x = (long)Math.Floor(rayPos.X);
            long y = (long)Math.Floor(rayPos.Y);
            long z = (long)Math.Floor(rayPos.Z);

            long stepX = Math.Sign(rayDir.X);
            long stepY = Math.Sign(rayDir.Y);
            long stepZ = Math.Sign(rayDir.Z);

            double tMaxX = (x + (stepX > 0 ? 1 : 0) - rayPos.X) / rayDir.X;
            double tMaxY = (y + (stepY > 0 ? 1 : 0) - rayPos.Y) / rayDir.Y;
            double tMaxZ = (z + (stepZ > 0 ? 1 : 0) - rayPos.Z) / rayDir.Z;

            double tDeltaX = Math.Abs(1 / rayDir.X);
            double tDeltaY = Math.Abs(1 / rayDir.Y);
            double tDeltaZ = Math.Abs(1 / rayDir.Z);

            int lastStepAxis = 0;

            for (double t = 0; t < maxDistance;)
            {
                Chunk* chunk = world.Get((int)(x >> 4), (int)(y >> 4), (int)(z >> 4));
                if (chunk == null || !chunk->InMemory)
                    return new RaycastHit { Hit = false }; // No chunk exists

                Block block = chunk->GetBlockInternal((int)(x & 15), (int)(y & 15), (int)(z & 15));
                if (block.Type != 0) // Hit a solid block
                {
                    Vector3 normal = lastStepAxis == 0 ? new Vector3(-stepX, 0, 0) :
                              lastStepAxis == 1 ? new Vector3(0, -stepY, 0) :
                              new Vector3(0, 0, -stepZ);

                    return new RaycastHit
                    {
                        Hit = true,
                        Position = new Vector3(x, y, z),
                        Normal = normal,
                        BlockX = (int)x,
                        BlockY = (int)y,
                        BlockZ = (int)z
                    };
                }

                if (tMaxX < tMaxY && tMaxX < tMaxZ)
                {
                    x += stepX;
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    lastStepAxis = 0;
                }
                else if (tMaxY < tMaxZ)
                {
                    y += stepY;
                    t = tMaxY;
                    tMaxY += tDeltaY;
                    lastStepAxis = 1;
                }
                else
                {
                    z += stepZ;
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    lastStepAxis = 2;
                }
            }

            return new RaycastHit { Hit = false };
        }
    }
}