namespace VoxelEngine.Physics
{
    using System.Numerics;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public struct RaycastHit
    {
        public Vector3 Position;
        public Vector3 Normal;
        public bool Hit;
        public int BlockX, BlockY, BlockZ;
    }

    public class PhysicsSystem : ISceneSystem
    {
        public string Name { get; } = "Physics System";

        public SystemFlags Flags { get; } = SystemFlags.Awake | SystemFlags.Destroy | SystemFlags.PhysicsUpdate;

        public void Awake(Scene scene)
        {
        }

        public void Destroy()
        {
        }

        public void FixedUpdate()
        {
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

            float tMaxX = ((x + (stepX > 0 ? 1 : 0)) - rayPos.X) / rayDir.X;
            float tMaxY = ((y + (stepY > 0 ? 1 : 0)) - rayPos.Y) / rayDir.Y;
            float tMaxZ = ((z + (stepZ > 0 ? 1 : 0)) - rayPos.Z) / rayDir.Z;

            float tDeltaX = Math.Abs(1 / rayDir.X);
            float tDeltaY = Math.Abs(1 / rayDir.Y);
            float tDeltaZ = Math.Abs(1 / rayDir.Z);

            for (float t = 0; t < maxDistance;)
            {
                Chunk chunk = world.Get(x >> 4, y >> 4, z >> 4);
                if (chunk == null || !chunk.InMemory)
                    return new RaycastHit { Hit = false }; // No chunk exists

                Block block = chunk.GetBlockInternal(x & 15, y & 15, z & 15);
                if (block.Type != 0) // Hit a solid block
                {
                    Vector3 normal;
                    if (tMaxX < tMaxY && tMaxX < tMaxZ) normal = new Vector3(-stepX, 0, 0);
                    else if (tMaxY < tMaxZ) normal = new Vector3(0, -stepY, 0);
                    else normal = new Vector3(0, 0, -stepZ);

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
                }
                else if (tMaxY < tMaxZ)
                {
                    y += stepY;
                    t = tMaxY;
                    tMaxY += tDeltaY;
                }
                else
                {
                    z += stepZ;
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                }
            }

            return new RaycastHit { Hit = false };
        }
    }
}