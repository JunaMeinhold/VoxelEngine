namespace HexaEngine.Objects
{
    using System.Numerics;
    using VoxelGen;

    public interface IChunkGenerator
    {
        public Chunk[] GenerateBatch(World world, Vector3 position);
    }
}