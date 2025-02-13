namespace VoxelEngine.Physics
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using HexaEngine.Queries.Generic;
    using System.Numerics;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.X86;
    using VoxelEngine.Core;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public interface IPhysicsComponent : IComponent
    {
        public void PreTick(PhysicsSystem system);

        public void PostTick(PhysicsSystem system);
    }

    public struct RaycastHit
    {
        public Vector3 Position;
        public Vector3 Normal;
        public bool Hit;
        public int BlockX, BlockY, BlockZ;
    }

    public enum ShapeType
    {
        Box,
    }

    public interface IShape
    {
        public ShapeType Type { get; }
    }

    public struct Pose
    {
        public Vector3 Position;
        public Vector3 Rotation;
    }

    public struct Shape
    {
        public readonly ShapeType Type;
        public Pose Pose;
    }

    public struct BoxShape : IShape
    {
        private readonly ShapeType type = ShapeType.Box;
        public Pose Pose;
        public Vector3 Size;

        public BoxShape(Vector3 size)
        {
            Size = size;
        }

        public readonly ShapeType Type => type;
    }

    public unsafe struct DynamicActor
    {
        internal Pose Pose;
        internal Pose lastPose;

        internal Vector3 LinearVelocity;
        internal Vector3 AngularVelocity;
        internal UnsafeList<Pointer<Shape>> shapes;

        public bool Grounded;

        public void SetPosition(Vector3 position)
        {
            if (Pose.Position == position) return;
            Grounded = false;
            Pose.Position = position;
            lastPose = Pose;
        }

        public void Move(Vector3 position)
        {
            if (Pose.Position == position) return;
            Grounded = false;
            lastPose = Pose;
            Pose.Position = position;
        }

        public Vector3 GetPosition() => Pose.Position;

        public void AddShape<T>(T shape) where T : unmanaged, IShape
        {
            var s = AllocT(shape);
            shapes.Add((Shape*)s);
        }
    }

    public unsafe class PhysicsSystem : ISceneSystem
    {
        private World world;
        private readonly ComponentTypeQuery<IPhysicsComponent> components = new();
        private UnsafeList<Pointer<DynamicActor>> actors;
        private bool awaked;

        public string Name { get; } = "Physics System";

        public SystemFlags Flags { get; } = SystemFlags.Awake | SystemFlags.Destroy | SystemFlags.PhysicsUpdate;

        public void Awake(Scene scene)
        {
            awaked = true;
            world = scene.Find<World>()!;
            components.OnAdded += OnAdded;
            components.OnRemoved += OnRemoved;
            scene.QueryManager.AddQuery(components);
        }

        private void OnRemoved(GameObject gameObject, IPhysicsComponent component)
        {
            if (awaked)
            {
                component.Destroy();
            }
        }

        private void OnAdded(GameObject gameObject, IPhysicsComponent component)
        {
            if (awaked)
            {
                component.Awake();
            }
        }

        public void Destroy()
        {
            components.OnAdded -= OnAdded;
            components.OnRemoved -= OnRemoved;
            components.Dispose();
            awaked = false;
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

            foreach (var component in components)
            {
                component.PreTick(this);
            }

            for (int i = 0; i < actors.Count; i++)
            {
                DynamicActor* actor = actors[i];
                Pose* pose = &actor->Pose;

                actor->LinearVelocity.Y += -0.81f * deltaTime;

                Vector3 desiredMovement = actor->LinearVelocity * deltaTime;
                Vector3 newPosition = SweepMove(actor, desiredMovement);

                pose->Position = newPosition;
                actor->lastPose = *pose;
            }

            foreach (var component in components)
            {
                component.PostTick(this);
            }
        }

        public unsafe Vector3 SweepMove(DynamicActor* actor, Vector3 movement)
        {
            Pose* actorPose = &actor->Pose;
            Pose* actorPoseLast = &actor->lastPose;

            Vector3 delta = actorPose->Position - actorPoseLast->Position;

            Vector3 startPos = actorPoseLast->Position;
            Vector3 endPos = startPos + movement + delta;

            Vector3 finalPosition = startPos;

            for (int i = 0; i < actor->shapes.Size; i++)
            {
                Shape* shape = actor->shapes[i];

                switch (shape->Type)
                {
                    case ShapeType.Box:
                        BoxShape* box = (BoxShape*)shape;
                        Vector3 worldBoxPosition = startPos + box->Pose.Position;

                        finalPosition.X = SweepAxis(worldBoxPosition, endPos, Vector3.UnitX, box, actor);
                        finalPosition.Y = SweepAxis(worldBoxPosition, endPos, Vector3.UnitY, box, actor);
                        finalPosition.Z = SweepAxis(worldBoxPosition, endPos, Vector3.UnitZ, box, actor);
                        break;

                    default:
                        break;
                }
            }

            return finalPosition;
        }

        private unsafe float SweepAxis(Vector3 start, Vector3 end, Vector3 axis, BoxShape* box, DynamicActor* actor)
        {
            Vector3 step = (end - start) * 0.1f;

            Vector3 s = start;
            Vector3 c = s;
            Vector3 e = end;
            /*
            while (Vector3.DistanceSquared(c, e) > 0.00001f)
            {
                c += step;
                if (Math.Abs(currentPos) > Math.Abs(targetPos))
                {
                    currentPos = targetPos;
                }

                if (IsBoxColliding(c, box))
                {
                    actor->LinearVelocity *= Vector3.One - axis;

                    return currentPos - step;
                }
            }*/

            return 0;
        }

        private unsafe bool IsBoxColliding(Vector3 actorPosition, BoxShape* box)
        {
            Vector3 worldBoxPosition = actorPosition + box->Pose.Position; // Apply local pose
            Vector3 halfExtents = box->Size * 0.5f; // Get half-extents for AABB check

            Vector3[] corners = new Vector3[]
            {
        worldBoxPosition + new Vector3(-halfExtents.X, -halfExtents.Y, -halfExtents.Z),
        worldBoxPosition + new Vector3( halfExtents.X, -halfExtents.Y, -halfExtents.Z),
        worldBoxPosition + new Vector3(-halfExtents.X,  halfExtents.Y, -halfExtents.Z),
        worldBoxPosition + new Vector3( halfExtents.X,  halfExtents.Y, -halfExtents.Z),
        worldBoxPosition + new Vector3(-halfExtents.X, -halfExtents.Y,  halfExtents.Z),
        worldBoxPosition + new Vector3( halfExtents.X, -halfExtents.Y,  halfExtents.Z),
        worldBoxPosition + new Vector3(-halfExtents.X,  halfExtents.Y,  halfExtents.Z),
        worldBoxPosition + new Vector3( halfExtents.X,  halfExtents.Y,  halfExtents.Z),
            };

            foreach (var corner in corners)
            {
                if (IsVoxelSolid(corner))
                {
                    return true; // Collision detected
                }
            }
            return false; // No collision
        }

        private bool IsVoxelSolid(Vector3 position)
        {
            // Convert position to voxel grid coordinates
            int voxelX = (int)Math.Floor(position.X);
            int voxelY = (int)Math.Floor(position.Y);
            int voxelZ = (int)Math.Floor(position.Z);

            return !world.IsNoBlock(voxelX, voxelY, voxelZ); // Query your voxel world
        }

        public unsafe bool HitCeiling(DynamicActor* actor, float maxCheckDistance = 0.5f)
        {
            Pose* pose = &actor->Pose;
            RaycastHit hit = CastRay(pose->Position, Vector3.UnitY, maxCheckDistance);

            if (hit.Hit)
            {
                pose->Position.Y = hit.Position.Y - 1;
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