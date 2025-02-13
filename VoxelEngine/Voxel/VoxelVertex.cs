namespace VoxelEngine.Voxel
{
    using System.Numerics;

    public struct VoxelVertex
    {
        public Vector3 Position;
        public int Data;
        public uint Color;

        public VoxelVertex(int data, Vector3 offset, uint color = uint.MaxValue)
        {
            Vector3 pos = new((data & (63)), ((data >> 6) & (63)), ((data >> 12) & (63)));
            Position = pos;
            Data = data;
            Color = color;
        }
    }
}