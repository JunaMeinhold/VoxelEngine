namespace App.Scripts
{
    using System.Numerics;
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
            world.WorldLoader.Upload(Device);
            Vector3 chunkPos = Scene.Camera.Transform.Position / Chunk.CHUNK_SIZE;
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
        }
    }
}