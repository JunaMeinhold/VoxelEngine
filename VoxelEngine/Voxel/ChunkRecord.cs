namespace VoxelEngine.Voxel
{
    using System.Numerics;

    public struct ChunkRecord
    {
        public ushort Type;
        public Vector3 Position;
        public byte Count;

        public ChunkRecord(ushort type, Vector3 position)
        {
            Type = type;
            Position = position;
        }
    }
}