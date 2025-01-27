namespace VoxelEngine.Voxel.WorldGen
{
    using System.Numerics;
    using VoxelEngine.Voxel;

    public interface IChunkGenerator : IDisposable
    {
        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Vector3 position);
    }
}