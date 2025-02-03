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

    public struct VoxelVertex2
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Bary;
        public int TextureID;

        public void Reset(float pX, float pY, float pZ, float nX, float nY, float nZ, float bX, float bY, int tID)
        {
            Position = new(pX, pY, pZ);
            Normal = new(nX, nY, nZ);
            Bary = new(bX, bY);
            TextureID = tID;
        }
    }
}