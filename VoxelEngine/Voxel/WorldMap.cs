namespace VoxelEngine.Voxel
{
    using System.Numerics;
    using Hexa.NET.Mathematics;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class WorldMap : GameObject
    {
        public ChunkArray Chunks;
        public const byte SHIFT = 5;
        public const int MASK = 0x1f;
        public const int CHUNK_AMOUNT_X = int.MaxValue;
        public const int CHUNK_AMOUNT_Y = 16;
        public const int CHUNK_AMOUNT_Z = int.MaxValue;
        public const int CHUNK_AMOUNT_X_MIN = int.MinValue;
        public const int CHUNK_AMOUNT_Y_MIN = 0;
        public const int CHUNK_AMOUNT_Z_MIN = int.MinValue;

        public string Path { get; protected set; }

        public WorldMap()
        {
        }

        public bool IsNoBlock(Vector3 pos)
        {
            return IsNoBlock(MathUtil.Round(pos.X), MathUtil.Round(pos.Y), MathUtil.Round(pos.Z));
        }

        public unsafe bool IsNoBlock(int x, int y, int z)
        {
            int xglobal = x / Chunk.CHUNK_SIZE;
            int xlocal = x % Chunk.CHUNK_SIZE;
            int yglobal = y / Chunk.CHUNK_SIZE;
            int ylocal = y % Chunk.CHUNK_SIZE;
            int zglobal = z / Chunk.CHUNK_SIZE;
            int zlocal = z % Chunk.CHUNK_SIZE;
            // If it is at the edge of the map, return true
            if (xglobal < CHUNK_AMOUNT_X_MIN || xglobal >= CHUNK_AMOUNT_X ||
                yglobal < CHUNK_AMOUNT_Y_MIN || yglobal >= CHUNK_AMOUNT_Y ||
                zglobal < CHUNK_AMOUNT_Z_MIN || zglobal >= CHUNK_AMOUNT_Z)
            {
                return true;
            }

            if (xlocal < 0 || xlocal >= Chunk.CHUNK_SIZE ||
                ylocal < 0 || ylocal >= Chunk.CHUNK_SIZE ||
                zlocal < 0 || zlocal >= Chunk.CHUNK_SIZE)
            {
                return true;
            }

            Chunk c = Chunks[xglobal, yglobal, zglobal];

            // To lower memory usage, a chunk is null if it has no blocks
            if (c == null)
            {
                return true;
            }

            // Chunk data accessed quickly using bit masks
            return c.Data[Extensions.MapToIndex(xlocal, ylocal, zlocal, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)].Type == Chunk.EMPTY;
        }

        public void Set(Chunk chunk, Vector3 pos)
        {
            if (pos.X < CHUNK_AMOUNT_X_MIN || pos.X >= CHUNK_AMOUNT_X ||
                pos.Y < CHUNK_AMOUNT_Y_MIN || pos.Y >= CHUNK_AMOUNT_Y ||
                pos.Z < CHUNK_AMOUNT_Z_MIN || pos.Z >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            Chunks[(int)pos.X, (int)pos.Y, (int)pos.Z] = chunk;
        }

        public void Set(Chunk chunk, float x, float y, float z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            Chunks[(int)x, (int)y, (int)z] = chunk;
        }

        public Chunk Get(Vector3 pos)
        {
            if (pos.X < CHUNK_AMOUNT_X_MIN || pos.X >= CHUNK_AMOUNT_X ||
                pos.Y < CHUNK_AMOUNT_Y_MIN || pos.Y >= CHUNK_AMOUNT_Y ||
                pos.Z < CHUNK_AMOUNT_Z_MIN || pos.Z >= CHUNK_AMOUNT_Z)
            {
                return null;
            }

            return Chunks[(int)pos.X, (int)pos.Y, (int)pos.Z];
        }

        public Chunk Get(float x, float y, float z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return null;
            }

            return Chunks[(int)x, (int)y, (int)z];
        }

        public void Set(ChunkSegment region)
        {
            if (region.Position.X < CHUNK_AMOUNT_X_MIN || region.Position.X >= CHUNK_AMOUNT_X ||
                region.Position.Y < CHUNK_AMOUNT_Y_MIN || region.Position.Y >= CHUNK_AMOUNT_Y)
            {
                return;
            }

            for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
            {
                Set(region.Chunks[i], region.Position.X, i, region.Position.Y);
            }
        }

        public ChunkSegment GetSegment(Vector3 pos)
        {
            if (pos.X < CHUNK_AMOUNT_X_MIN || pos.X >= CHUNK_AMOUNT_X ||
                pos.Y < CHUNK_AMOUNT_Y_MIN || pos.Y >= CHUNK_AMOUNT_Y ||
                pos.Z < CHUNK_AMOUNT_Z_MIN || pos.Z >= CHUNK_AMOUNT_Z)
            {
                return default;
            }

            return ChunkSegment.CreateFrom(this, pos);
        }

        public ChunkSegment GetSegment(float x, float z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                z < CHUNK_AMOUNT_Y_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return default;
            }

            return ChunkSegment.CreateFrom(this, x, z);
        }
    }
}