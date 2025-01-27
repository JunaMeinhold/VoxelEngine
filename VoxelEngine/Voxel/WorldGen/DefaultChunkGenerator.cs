namespace VoxelEngine.Voxel.WorldGen
{
    using System.Numerics;
    using VoxelEngine.Mathematics.Noise;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.WorldGen.Biomes;
    using static VoxelEngine.Voxel.Blocks.BlockRegistry;

    public class DefaultChunkGenerator : IChunkGenerator
    {
        private readonly GenericNoise genericNoise;
        private readonly PerlinNoise perlinNoise;
        private readonly SimplexNoise simplexNoise;
        private readonly int seed;

        public DefaultChunkGenerator(int seed)
        {
            genericNoise = new(seed);
            perlinNoise = new();
            simplexNoise = new(seed);
            this.seed = seed;

            Biome plains = new()
            {
                Name = "Plains",
                Octaves = 3,
                Persistence = 0.5f,
                Amplitude = 1,
                MaxHeight = 120,
                MinHeight = 100,
                Redistribution = 5,
                WaterHeight = 50,
            };

            Biome hills = new()
            {
                Name = "Hills",
                Octaves = 3,
                Persistence = 0.5f,
                Amplitude = 1,
                MaxHeight = 240,
                MinHeight = 120,
                Redistribution = 10,
                WaterHeight = 50,
            };

            Biome mountains = new()
            {
                Name = "Mountains",
                Octaves = 3,
                Persistence = 0.5f,
                Amplitude = 2,
                MaxHeight = 240,
                MinHeight = 160,
                Redistribution = 10,
                WaterHeight = 50,
            };

            //Biomes.Add(plains);
            Biomes.Add(hills);
            //Biomes.Add(mountains);
        }

        public List<Biome> Biomes { get; } = new();

        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Vector3 position)
        {
            for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk chunk = new(world, (int)position.X, i, (int)position.Z, true);
                world.Set(chunk, new(position.X, i, position.Z));
                chunks[i] = chunk;
            }

            Vector3 chunkPos = position * Chunk.CHUNK_SIZE;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    float globalX = chunkPos.X + x;
                    float globalZ = chunkPos.Z + z;

                    BiomeData data = BiomeData.GetBlendedBiomeData(perlinNoise, Biomes, globalX, globalZ);

                    float height = data.ComputeHeight(perlinNoise, globalX, globalZ);

                    if (height <= 0)
                    {
                        continue;
                    }

                    CaveCoefficients coefficients = CaveCoefficients.Generate(perlinNoise, globalX, globalZ, 2, 100);

                    for (int h = 0; h < (int)height && h < Chunk.CHUNK_SIZE_SQUARED; h++)
                    {
                        float caveNoiseValue = coefficients.ComputeValue(h);

                        if (caveNoiseValue > 0)
                        {
                            continue;
                        }

                        MapToChunks(ref chunks, new Vector3(x, h, z), GetBlock(h, (int)height));
                    }
                }
            }
        }

        public static unsafe void MapToChunks(ref ChunkSegment.ChunkArray chunks, Vector3 pos, Block block)
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
            chunks[cheight].Data[new Vector3(pos.X, height, pos.Z).MapToIndex(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)] = block;
        }

        public static Block GetBlock(int height, int maxheight)
        {
            if (height == maxheight - 1)
            {
                return GetBlockByName("Grass");
            }
            else if (height > maxheight - 5)
            {
                return GetBlockByName("Dirt");
            }
            else
            {
                return GetBlockByName("Stone");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~DefaultChunkGenerator()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}