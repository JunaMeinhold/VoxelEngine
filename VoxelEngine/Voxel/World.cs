namespace VoxelEngine.Voxel
{
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using Vortice.Direct3D11;
    using VoxelEngine.Voxel.WorldGen;

    public class World : WorldMap
    {
        //private Vector3 CurrentPlayerChunkPos;
        //private bool invalidate = true;

        public World(string path)
        {
            Name = nameof(World);
            DirectoryInfo dir = Directory.CreateDirectory(path);
            if (!dir.Exists)
            {
                dir.Create();
            }

            Chunks = new();
            Path = dir.FullName;
        }

        public IChunkGenerator Generator { get; set; }

        public Player Player { get; set; }

        public VoxelHelper VoxelHelper { get; } = new(Matrix4x4.Identity);

        public IReadOnlyList<Chunk> LoadedChunks => WorldLoader.LoadedChunks;

        public IReadOnlyList<ChunkRegion> LoadedChunkRegions => WorldLoader.LoadedChunkRegions;

#if !USE_LEGACY_LOADER
        public IReadOnlyList<RenderRegion> LoadedRenderRegions => WorldLoader.LoadedRenderRegions;
#endif

        public WorldLoader WorldLoader;

        public static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            int xglobal = (int)Math.Floor((float)x / Chunk.CHUNK_SIZE);
            int xlocal = Mod(x, Chunk.CHUNK_SIZE);
            int yglobal = (int)Math.Floor((float)y / Chunk.CHUNK_SIZE);
            int ylocal = Mod(y, Chunk.CHUNK_SIZE);
            int zglobal = (int)Math.Floor((float)z / Chunk.CHUNK_SIZE);
            int zlocal = Mod(z, Chunk.CHUNK_SIZE);
            // If it is at the edge of the map, return true
            if (xglobal < CHUNK_AMOUNT_X_MIN || xglobal >= CHUNK_AMOUNT_X ||
                yglobal < CHUNK_AMOUNT_Y_MIN || yglobal >= CHUNK_AMOUNT_Y ||
                zglobal < CHUNK_AMOUNT_Z_MIN || zglobal >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            if (xlocal < 0 || xlocal >= Chunk.CHUNK_SIZE ||
                ylocal < 0 || ylocal >= Chunk.CHUNK_SIZE ||
                zlocal < 0 || zlocal >= Chunk.CHUNK_SIZE)
            {
                return;
            }

            // Chunk accessed quickly using bitwise shifts
            Chunk c = Chunks[xglobal, yglobal, zglobal];

            // To lower memory usage, a chunk is null if it has no blocks
            if (c == null)
            {
                return;
            }

            // Chunk data accessed quickly using bit masks
            c.SetBlockInternal(block, xlocal, ylocal, zlocal);
            UpdateChunk(xglobal, yglobal, zglobal);
            UpdateChunk(xglobal + 1, yglobal, zglobal);
            UpdateChunk(xglobal, yglobal + 1, zglobal);
            UpdateChunk(xglobal, yglobal, zglobal + 1);
            UpdateChunk(xglobal - 1, yglobal, zglobal);
            UpdateChunk(xglobal, yglobal - 1, zglobal);
            UpdateChunk(xglobal, yglobal, zglobal - 1);
        }

        public Block GetBlock(int x, int y, int z)
        {
            int xglobal = (int)Math.Floor((float)x / Chunk.CHUNK_SIZE);
            int xlocal = Mod(x, Chunk.CHUNK_SIZE);
            int yglobal = (int)Math.Floor((float)y / Chunk.CHUNK_SIZE);
            int ylocal = Mod(y, Chunk.CHUNK_SIZE);
            int zglobal = (int)Math.Floor((float)z / Chunk.CHUNK_SIZE);
            int zlocal = Mod(z, Chunk.CHUNK_SIZE);
            // If it is at the edge of the map, return true
            if (xglobal < CHUNK_AMOUNT_X_MIN || xglobal >= CHUNK_AMOUNT_X ||
                yglobal < CHUNK_AMOUNT_Y_MIN || yglobal >= CHUNK_AMOUNT_Y ||
                zglobal < CHUNK_AMOUNT_Z_MIN || zglobal >= CHUNK_AMOUNT_Z)
            {
                return default;
            }

            if (xlocal < 0 || xlocal >= Chunk.CHUNK_SIZE ||
                ylocal < 0 || ylocal >= Chunk.CHUNK_SIZE ||
                zlocal < 0 || zlocal >= Chunk.CHUNK_SIZE)
            {
                return default;
            }

            // Chunk accessed quickly using bitwise shifts
            Chunk c = Chunks[xglobal, yglobal, zglobal];

            return c.GetBlockInternal(xlocal, ylocal, zlocal);
        }

        public static bool InChunkBounds(int x, int y, int z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return false;
            }

            return true;
        }

        public void UpdateChunk(int x, int y, int z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            WorldLoader.Dispatch(Chunks[x, y, z]);
        }

        public void LoadFromDisk(Vector3 pos)
        {
            for (int i = 0; i < CHUNK_AMOUNT_Y; i++)
            {
                Chunk chunk = new(this, (int)pos.X, i, (int)pos.Z);
                Set(chunk, (int)pos.X, i, (int)pos.Z);
            }
        }

        public override void Initialize(ID3D11Device device)
        {
            Player = Scene.GetElementByType<Player>();
            base.Initialize(device);
            WorldLoader = new(this);
            Generator.Dispose();
        }

        public override void Uninitialize()
        {
            base.Uninitialize();
            WorldLoader.Dispose();
            Chunks.Clear();
            Chunks = null;
        }
    }
}