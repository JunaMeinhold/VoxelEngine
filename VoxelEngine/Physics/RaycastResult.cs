namespace VoxelEngine.Physics
{
    using System.Numerics;
    using BepuPhysics.Collidables;

    public struct RaycastResult
    {
        public bool Hit;
        public CollidableReference Collidable;
        public float T;
        public int ChildIndex;
        public Vector3 Normal;
    }
}