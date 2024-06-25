namespace VoxelEngine.Voxel
{
    public unsafe struct ChunkHelper
    {
        public bool* visitXN = AllocTAndZero<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitXP = AllocTAndZero<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitZN = AllocTAndZero<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitZP = AllocTAndZero<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitYN = AllocTAndZero<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitYP = AllocTAndZero<bool>(Chunk.CHUNK_SIZE_CUBED);

        public ChunkHelper()
        {
        }

        public void Release()
        {
            Free(visitXN);
            Free(visitXP);
            Free(visitZN);
            Free(visitZP);
            Free(visitYN);
            Free(visitYP);
            this = default;
        }
    }
}