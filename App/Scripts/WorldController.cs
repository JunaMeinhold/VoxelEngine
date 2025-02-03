namespace App.Scripts
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;
    using VoxelEngine.Scripting;
    using VoxelEngine.Voxel;

    public class WorldController : ScriptComponent
    {
        private Vector3 CurrentPlayerChunkSegmentPos;
        private Vector3 CurrentPlayerChunkPos;
        private Vector3 CurrentPlayerLocalChunkPos;
        private Vector3 CurrentPlayerPos;
        private bool invalidate = true;
        private World world;

        public override void Awake()
        {
            world = GameObject as World;
        }

        public override void Destroy()
        {
        }

        public override void FixedUpdate()
        {
            var context = D3D11DeviceManager.GraphicsContext;
            world.WorldLoader.Upload(context);
            Vector3 pos = Scene.Camera.Transform.Position;

            float x = pos.X % Chunk.CHUNK_SIZE;
            float y = pos.Y % Chunk.CHUNK_SIZE;
            float z = pos.Z % Chunk.CHUNK_SIZE;

            CurrentPlayerLocalChunkPos = new((int)x, (int)y, (int)z);

            Vector3 chunkPos = pos - new Vector3(x, y, z);
            if (x < 0)
            {
                chunkPos.X -= Chunk.CHUNK_SIZE;
            }
            if (y < 0)
            {
                chunkPos.Y -= Chunk.CHUNK_SIZE;
            }
            if (z < 0)
            {
                chunkPos.Z -= Chunk.CHUNK_SIZE;
            }

            chunkPos /= Chunk.CHUNK_SIZE;

            CurrentPlayerChunkPos = new Vector3((int)chunkPos.X, (int)chunkPos.Y, (int)chunkPos.Z);
            CurrentPlayerPos = CurrentPlayerLocalChunkPos + CurrentPlayerChunkPos * Chunk.CHUNK_SIZE;
            chunkPos = new Vector3((int)chunkPos.X, 0, (int)chunkPos.Z);

            if (chunkPos.X == CurrentPlayerChunkSegmentPos.X & chunkPos.Z == CurrentPlayerChunkSegmentPos.Z & !invalidate)
            {
                return;
            }

            invalidate = false;
            CurrentPlayerChunkSegmentPos = chunkPos;
            world.WorldLoader.Dispatch(chunkPos);
        }

        public override void Update()
        {
            ImGui.Text($"Chunk Segment: {CurrentPlayerChunkSegmentPos}");
            ImGui.Text($"Chunk: {CurrentPlayerChunkPos}");
            ImGui.Text($"Local Position: {CurrentPlayerLocalChunkPos}");
            ImGui.Text($"Position: {CurrentPlayerPos}");
            ImGui.Separator();
            ImGui.Text($"Loader: loads/unloads/updates: {world.WorldLoader.LoadQueueCount}/{world.WorldLoader.UnloadQueueCount}/{world.WorldLoader.UpdateQueueCount}, gen {world.WorldLoader.GenerationQueueCount}, uploads: {world.WorldLoader.UploadQueueCount}, {(world.WorldLoader.Idle ? "Idle" : "")}");
            ImGui.Text($"IO: loads/unloads/saves: {world.WorldLoader.LoadIOQueueCount}/{world.WorldLoader.UnloadIOQueueCount}/{world.WorldLoader.SaveIOQueueCount}, {(world.WorldLoader.IOIdle ? "Idle" : "")}");
            ImGui.Text($"Loaded Render Regions: {world.WorldLoader.RenderRegionCount}");
            ImGui.Text($"Loaded Chunk Segments: {world.WorldLoader.ChunkSegmentCount}");
            ImGui.Text($"Loaded Chunks: {world.WorldLoader.ChunkCount}");

            var directionalLight = Scene.LightSystem.ActiveDirectionalLight;

            if (directionalLight != null)
            {
                Vector3 rot = directionalLight.Transform.Rotation;
                rot.Y = 360 * Time.GameTimeNormalized - 90;
                float ro = rot.Y - 180F;
                directionalLight.Transform.Rotation = rot.NormalizeEulerAngleDegrees();
                if (rot.Y > 45 && rot.Y < 135)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1);
                }
                else if (rot.Y > 135 && rot.Y < 225)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(1, 0.2f, (rot.Y - 135) / 90);
                }
                else if (ro > 45 && ro < 135)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 0.2f;
                }
                else if (ro > 135 && ro < 225)
                {
                    directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(0.2f, 1, (ro - 135) / 90);
                }
                directionalLight.Color *= 5;
            }
        }
    }
}