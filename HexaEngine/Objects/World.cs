namespace HexaEngine.Objects
{
    using HexaEngine.Audio;
    using HexaEngine.Objects.Renderers;
    using HexaEngine.Objects.VoxelGen;
    using HexaEngine.Physics;
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Scenes.Objects;
    using HexaEngine.Windows;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using Vortice.Mathematics;
    using Vortice.XAudio2;

    public class World : WorldMap, IScriptObject, IFrameScriptObject
    {
        private Sound sound;
        private Vector3 CurrentPlayerChunkPos;
        private bool invalidate = true;

        public World(string path)
        {
            Emitter.Position = Vector3.One;
            DeferredRenderer.Light = Sun;
            var dir = Directory.CreateDirectory(path);
            if (!dir.Exists)
            {
                dir.Create();
            }
            Chunks = new Chunk[CHUNK_AMOUNT_X, CHUNK_AMOUNT_Y, CHUNK_AMOUNT_Z];
            Path = dir.FullName;
            Emitter.ChannelCount = 1;
            Emitter.CurveDistanceScaler = 1;
            Emitter.OrientTop = Vector3.UnitY;
            Emitter.OrientFront = Vector3.UnitZ;

            sound = ResourceManager.LoadSound("test.wav");
            sound.Emitter = Emitter;
        }

        public IObjectRenderer Renderer { get; } = new WorldRenderer();

        public Matrix4x4 Transform { get; } = Matrix4x4.Identity;

        public IChunkGenerator Generator { get; set; }

        public Skybox Skybox { get; set; }

        public Player Player { get; set; }

        public int RenderDistance { get; set; } = 16;

        public int Seed { get; private set; }

        public int Time { get; private set; }

        public int TimeScale { get; set; } = 1;

        public Sun Sun { get; } = new();

        public Emitter Emitter { get; } = new();

        public VoxelHelper VoxelHelper { get; } = new(Matrix4x4.Identity);

        public List<Chunk> LoadedChunks => WorldLoader.LoadedChunks;

        public WorldLoader WorldLoader;

        public void Raycast(Func<RaycastResult, bool> callback, Ray ray, float distance)
        {
            var hints = VoxelHelper.Traverse(ray, distance);
            var hasHit = false;
            foreach (Vector3 hint in hints)
            {
                var aabb = new BoundingBox(hint - Vector3.One / 2, hint + Vector3.One / 2);

                if (ray.Intersects(aabb) != null && !IsNoBlock(hint))
                {
                    if (callback.Invoke(new RaycastResult() { Ray = ray, Position = hint, Hit = true }))
                    {
                        hasHit = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            if (!hasHit)
            {
                _ = callback.Invoke(new RaycastResult() { Ray = ray, Hit = false });
            }
        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            var xglobal = x / Chunk.CHUNK_SIZE;
            var xlocal = x % Chunk.CHUNK_SIZE;
            var yglobal = y / Chunk.CHUNK_SIZE;
            var ylocal = y % Chunk.CHUNK_SIZE;
            var zglobal = z / Chunk.CHUNK_SIZE;
            var zlocal = z % Chunk.CHUNK_SIZE;
            // If it is at the edge of the map, return true
            if (xglobal < 0 || xglobal >= MAP_SIZE_X ||
                yglobal < 0 || yglobal >= MAP_SIZE_Y ||
                zglobal < 0 || zglobal >= MAP_SIZE_Z)
                return;
            if (xlocal < 0 || xlocal >= Chunk.CHUNK_SIZE ||
                ylocal < 0 || ylocal >= Chunk.CHUNK_SIZE ||
                zlocal < 0 || zlocal >= Chunk.CHUNK_SIZE)
                return;

            // Chunk accessed quickly using bitwise shifts
            var c = Chunks[xglobal, yglobal, zglobal];

            // To lower memory usage, a chunk is null if it has no blocks
            if (c == null)
                return;

            // Chunk data accessed quickly using bit masks
            c.data[Extensions.MapToIndex(xlocal, ylocal, zlocal, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE)] = block;
            UpdateChunk(xglobal, yglobal, zglobal);
            UpdateChunk(xglobal + 1, yglobal, zglobal);
            UpdateChunk(xglobal, yglobal + 1, zglobal);
            UpdateChunk(xglobal, yglobal, zglobal + 1);
            UpdateChunk(xglobal - 1, yglobal, zglobal);
            UpdateChunk(xglobal, yglobal - 1, zglobal);
            UpdateChunk(xglobal, yglobal, zglobal - 1);
        }

        public void UpdateChunk(int x, int y, int z)
        {
            if (x < 0 || x >= MAP_SIZE_X ||
                  y < 0 || y >= MAP_SIZE_Y ||
                  z < 0 || z >= MAP_SIZE_Z)
                return;
            WorldLoader.Dispatch(Chunks[x, y, z]);
        }

        public void LoadFromDisk(Vector3 pos)
        {
            for (int i = 0; i < MAP_SIZE_Y; i++)
            {
                var chunk = new Chunk(this, (int)pos.X, i, (int)pos.Z);
                Set(chunk, (int)pos.X, i, (int)pos.Z);
            }
        }

        public void Update()
        {
            if (sound.Playing)
            {
                sound.Tick();
            }
            else
            {
                sound.Play();
            }

            WorldLoader.Upload();
            var chunkPos = Player.Camera.Position / Chunk.CHUNK_SIZE;
            chunkPos = new Vector3((int)chunkPos.X, 0, (int)chunkPos.Z);
            if (chunkPos.X == CurrentPlayerChunkPos.X & chunkPos.Z == CurrentPlayerChunkPos.Z & !invalidate) return;
            invalidate = false;
            CurrentPlayerChunkPos = chunkPos;
            WorldLoader.Dispatch(chunkPos);
        }

        public void Initialize()
        {
            WorldLoader = new(this);
        }

        public void Uninitialize()
        {
            Skybox?.Model.Dispose();
            Skybox?.Texture.Dispose();
            WorldLoader.Dispose();
        }

        public void Awake()
        {
        }

        public void Sleep()
        {
        }

        public void UpdateFixed()
        {
            Time += TimeScale;
            if (Time >= 24000)
            {
                Time = 0;
            }
            Sun.Update(Player.Camera, Time);
        }
    }
}