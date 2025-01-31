namespace VoxelEngine.Voxel.WorldGen
{
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Voxel;
    using static VoxelEngine.Voxel.Blocks.BlockRegistry;

    public class FlatChunkGenerator : IChunkGenerator
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int Height { get; set; } = 4;

        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Vector3 position)
        {
            for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk chunk = new(world, (int)position.X, i, (int)position.Z, true);
                world.Set(chunk, new(position.X, i, position.Z));
                chunks[i] = chunk;
            }

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    float he = Height;
                    if (he <= 0)
                    {
                        continue;
                    }

                    for (int h = 0; h < (int)he & h < Chunk.CHUNK_SIZE_SQUARED; h++)
                    {
                        MapToChunks(ref chunks, new Point3(x, h, z), GetBlock(h, (int)he));
                    }
                }
            }
        }

        private static float Saturate(float value)
        {
            return (float)((value + 1.0) / 2.0);
        }

        public static unsafe void MapToChunks(ref ChunkSegment.ChunkArray chunks, Point3 pos, Block block)
        {
            int index = pos.Y >> 4;
            int height = pos.Y & 15;
            chunks[index].MinY[new Point2(pos.X, pos.Z).MapToIndex()] = 0;
            chunks[index].MaxY[new Point2(pos.X, pos.Z).MapToIndex()] = (byte)(height + 1);
            chunks[index].Data[new Point3(pos.X, height, pos.Z).MapToIndex()] = block;
        }

        public static Block GetBlock(int height, int maxheight)
        {
            if (height == maxheight - 1)
            {
                return GetBlockByName("Grass");
            }
            else
            {
                return GetBlockByName("Dirt");
            }
        }
    }
}