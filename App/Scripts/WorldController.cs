namespace App.Scripts
{
    using System.Numerics;
    using HexaEngine.ImGuiNET;
    using VoxelEngine.Scripting;
    using VoxelEngine.Voxel;

    public class WorldController : ScriptComponent
    {
        private Vector3 CurrentPlayerChunkPos;
        private bool invalidate = true;
        private World world;

        public override void Awake()
        {
            world = Parent as World;
        }

        public override void Destroy()
        {
        }

        public override void FixedUpdate()
        {
            world.WorldLoader.Upload(Device, Device.ImmediateContext);
            Vector3 pos = Scene.Camera.Transform.Position;

            float x = pos.X % Chunk.CHUNK_SIZE;
            float y = pos.Y % Chunk.CHUNK_SIZE;
            float z = pos.Z % Chunk.CHUNK_SIZE;

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
            chunkPos = new Vector3((int)chunkPos.X, 0, (int)chunkPos.Z);

            if (chunkPos.X == CurrentPlayerChunkPos.X & chunkPos.Z == CurrentPlayerChunkPos.Z & !invalidate)
            {
                return;
            }

            invalidate = false;
            CurrentPlayerChunkPos = chunkPos;
            world.WorldLoader.Dispatch(chunkPos);
        }

        public override void Update()
        {
            ImGui.Text($"Current Chunk: {CurrentPlayerChunkPos}");
            ImGui.Text($"Chunk Loader: Idle: {world.WorldLoader.Idle}");
            ImGui.Text($"Chunk Update Queue: updates: {world.WorldLoader.UpdateQueueCount}, uploads: {world.WorldLoader.UploadQueueCount}");
            ImGui.Text($"Chunk Unload Queue: unloads: {world.WorldLoader.UnloadQueueCount}, io: {world.WorldLoader.UnloadIOQueueCount}");
            ImGui.Text($"Chunk IO Queue: saves: unload: {world.WorldLoader.UnloadIOQueueCount}");
            ImGui.Text($"Loaded Render Regions: {world.WorldLoader.RenderRegionCount}");
            ImGui.Text($"Loaded Chunk Regions: {world.WorldLoader.ChunkRegionCount}");
            ImGui.Text($"Loaded Chunks: {world.WorldLoader.ChunkCount}");
        }
    }
}