namespace VoxelEngine.Voxel
{
    using System.Numerics;

    public struct VoxelVertex
    {
        public int Data;
        public Vector3 Offset;

        public VoxelVertex(int data, Vector3 offset)
        {
            Data = data;
            Offset = offset;
        }
    }
}