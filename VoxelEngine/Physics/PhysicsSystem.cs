namespace VoxelEngine.Physics
{
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
            Vector3 rayPos = origin;
            Vector3 rayDir = Vector3.Normalize(direction);

            int x = (int)Math.Floor(rayPos.X);
            int y = (int)Math.Floor(rayPos.Y);
            int z = (int)Math.Floor(rayPos.Z);

            int stepX = Math.Sign(rayDir.X);
            int stepY = Math.Sign(rayDir.Y);
            int stepZ = Math.Sign(rayDir.Z);

            float tMaxX = (x + (stepX > 0 ? 1 : 0) - rayPos.X) / rayDir.X;
            float tMaxY = (y + (stepY > 0 ? 1 : 0) - rayPos.Y) / rayDir.Y;
            float tMaxZ = (z + (stepZ > 0 ? 1 : 0) - rayPos.Z) / rayDir.Z;

            float tDeltaX = Math.Abs(1 / rayDir.X);
            float tDeltaY = Math.Abs(1 / rayDir.Y);
            float tDeltaZ = Math.Abs(1 / rayDir.Z);

            int lastStepAxis = 0;

            for (float t = 0; t < maxDistance;)
            {
                Chunk* chunk = world.Get(x >> 4, y >> 4, z >> 4);
                if (chunk == null || !chunk->InMemory)
                    return new RaycastHit { Hit = false }; // No chunk exists

                Block block = chunk->GetBlockInternal(x & 15, y & 15, z & 15);
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
                        BlockX = x,
                        BlockY = y,
                        BlockZ = z
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