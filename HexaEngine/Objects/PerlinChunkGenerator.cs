namespace HexaEngine.Objects
{
    using HexaEngine.Extensions;
    using System.Numerics;
    using VoxelGen;
    using static HexaEngine.Objects.Registry;

    public class PerlinChunkGenerator : IChunkGenerator
    {
        private readonly FastNoise fastNoise;

        public PerlinChunkGenerator(int seed)
        {
            fastNoise = FastNoise.FromEncodedNodeTree("IAAQAI/C9T0NAAUAAAAAAABABwAAAAAAPwAAAAAAAAAAgD8A7FE4vgC4HoU/");
            Seed = seed;
        }

        public int MaxHeight { get; set; } = 80;

        public int MinHeight { get; set; } = 5;
        public int Seed { get; }

        public int WaterHeight = 14;

        public Chunk[] GenerateBatch(World world, Vector3 position)
        {
            Chunk[] chunks = new Chunk[WorldMap.CHUNK_AMOUNT_Y];
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = new(world, (int)position.X, i, (int)position.Z);
                world.Set(chunk, new(position.X, i, position.Z));
                chunks[i] = chunk;
            }

            var realpos = position * Chunk.CHUNK_SIZE;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    var val = fastNoise.GenSingle2D(0.02f * (realpos.X + x), 0.02f * (realpos.Z + z), Seed);
                    var he = (MaxHeight - MinHeight) * Saturate(val) + MinHeight;
                    if (he <= 0) continue;
                    for (var h = 0; h < (int)he & h < Chunk.CHUNK_SIZE_SQUARED; h++)
                    {
                        MapToChunks(chunks, new Vector3(x, h, z), GetBlock(h, (int)he));
                    }
                }
            }

            return chunks;
        }

        private static float Saturate(float value)
        {
            return (float)((value + 1.0) / 2.0);
        }

        public static void MapToChunks(Chunk[] chunks, Vector3 pos, Block block)
        {
            int cheight = 0;
            int height = (int)pos.Y;
            while (height >= Chunk.CHUNK_SIZE)
            {
                height -= Chunk.CHUNK_SIZE;
                cheight++;
            }
            chunks[cheight].MinY[new Vector2(pos.X, pos.Z).MapToIndex(Chunk.CHUNK_SIZE)] = 0;
            chunks[cheight].MaxY[new Vector2(pos.X, pos.Z).MapToIndex(Chunk.CHUNK_SIZE)] = (byte)(height + 1);
            chunks[cheight].data[new Vector3(pos.X, height, pos.Z).MapToIndex(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)] = block;
        }

        public static Block GetBlock(int height, int maxheight)
        {
            if (height == maxheight - 1)
            {
                return Blocks.Grass;
            }
            else if (height > maxheight - 5)
            {
                return Blocks.Dirt;
            }
            else
            {
                return Blocks.Stone;
            }
        }
    }
}