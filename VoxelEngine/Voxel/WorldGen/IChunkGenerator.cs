namespace VoxelEngine.Voxel.WorldGen
{
    using System.Numerics;
    using VoxelEngine.Voxel;

    public interface IChunkGenerator : IDisposable
    {
        public Chunk[] GenerateBatch(World world, Vector3 position);
    }
}