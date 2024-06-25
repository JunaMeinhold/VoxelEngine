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
                MaxHeight = 80,
                MinHeight = 60,
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

            Biomes.Add(plains);
            Biomes.Add(hills);
            //Biomes.Add(mountains);
        }

        public List<Biome> Biomes { get; } = new();

        public void GetBiomeData(float x, float y, out int majorBiome, out int minorBiome, out float blend)
        {
            // Maybe tweak values.
            float v = perlinNoise.OctavePerlin2D(0.002f * x, 0.002f * y, 10, 0.25f, 3);

            // Saturate value to 0 .. 1 range.
            v = SaturateOctave(v, 10, 0.25f, 3);

            // Redistribution.
            v = MathF.Pow(v, 1f);

            // Lerp between 1 and count and remap to index.
            v = Lerp(1, Biomes.Count, Clamp01(v)) - 1;

            majorBiome = (int)MathF.Truncate(v);
            minorBiome = (int)MathF.Round(v, MidpointRounding.ToEven);

            blend = v - majorBiome;

            // remap from 0.5 .. 1 to 0 .. 0.5
            if (blend >= 0.5f)
            {
                blend -= 0.5f;
            }
            else
            {
                blend = 0.5f - blend;
            }

            // remap from 0 .. 0.5 to 0 .. 1
            blend *= 2f;
        }

        private static float Frac(float f)
        {
            return f - float.Truncate(f);
        }

        private static float Lerp(float a, float b, float v)
        {
            return a + v * (b - a);
        }

        private static float Smoothstep(float a, float b, float v)
        {
            // Scale, and clamp x to 0..1 range
            v = Clamp01((v - a) / (b - a));

            return v * v * (3.0f - 2.0f * v);
        }

        private static float Saturate(float value)
        {
            return (float)((value + 1.0) / 2.0);
        }

        private static float SaturateOctave(float value, int octaves, float persistence, float amplitude)
        {
            float result = 0;

            for (int i = 0; i < octaves; i++)
            {
                result += amplitude;
                amplitude *= persistence;
            }

            return value / result;
        }

        private static float Clamp01(float value)
        {
            return Math.Clamp(value, 0.0f, 1.0f);
        }

        private static float SaturateClamp01(float value)
        {
            value = Clamp01(value);
            return Saturate(value);
        }

        public Chunk[] GenerateBatch(World world, Vector3 position)
        {
            Chunk[] chunks = new Chunk[WorldMap.CHUNK_AMOUNT_Y];
            for (int i = 0; i < chunks.Length; i++)
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

                    GetBiomeData(globalX, globalZ, out int majorBiomeIdx, out int minorBiomeIdx, out float blend);

                    Biome majorBiome = Biomes[majorBiomeIdx];
                    Biome minorBiome = Biomes[minorBiomeIdx];

                    float smoothstepBlend = Smoothstep(0, 1, blend);

                    int octaves = (int)Lerp(majorBiome.Octaves, minorBiome.Octaves, smoothstepBlend);
                    float persistence = Lerp(majorBiome.Persistence, minorBiome.Persistence, smoothstepBlend);
                    float amplitude = Lerp(majorBiome.Amplitude, minorBiome.Amplitude, smoothstepBlend);
                    float redistribution = Lerp(majorBiome.Redistribution, minorBiome.Redistribution, smoothstepBlend);

                    int minHeight = (int)Lerp(majorBiome.MinHeight, minorBiome.MinHeight, smoothstepBlend);
                    int maxHeight = (int)Lerp(majorBiome.MaxHeight, minorBiome.MaxHeight, smoothstepBlend);

                    float v = perlinNoise.OctavePerlin2D(0.002f * globalX, 0.002f * globalZ, octaves, persistence, amplitude);

                    v = SaturateOctave(v, octaves, persistence, amplitude);

                    // Redistribution
                    v = MathF.Pow(v, redistribution);

                    float height = Lerp(minHeight, maxHeight, Clamp01(v));

                    if (height <= 0)
                    {
                        continue;
                    }

                    for (int h = 0; h < (int)height & h < Chunk.CHUNK_SIZE_SQUARED; h++)
                    {
                        MapToChunks(chunks, new Vector3(x, h, z), GetBlock(h, (int)height));
                    }
                }
            }

            return chunks;
        }

        public static unsafe void MapToChunks(Chunk[] chunks, Vector3 pos, Block block)
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