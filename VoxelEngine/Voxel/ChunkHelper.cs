namespace VoxelEngine.Voxel
{
    using System;

    public class ChunkHelper
    {
        public bool[] visitXN = new bool[Chunk.CHUNK_SIZE_CUBED];
        public bool[] visitXP = new bool[Chunk.CHUNK_SIZE_CUBED];
        public bool[] visitZN = new bool[Chunk.CHUNK_SIZE_CUBED];
        public bool[] visitZP = new bool[Chunk.CHUNK_SIZE_CUBED];
        public bool[] visitYN = new bool[Chunk.CHUNK_SIZE_CUBED];
        public bool[] visitYP = new bool[Chunk.CHUNK_SIZE_CUBED];

        public void Reset()
        {
            // Clearing is faster than allocating a new array
            Array.Clear(visitXN, 0, Chunk.CHUNK_SIZE_CUBED);
            Array.Clear(visitXP, 0, Chunk.CHUNK_SIZE_CUBED);
            Array.Clear(visitYN, 0, Chunk.CHUNK_SIZE_CUBED);
            Array.Clear(visitYP, 0, Chunk.CHUNK_SIZE_CUBED);
            Array.Clear(visitZN, 0, Chunk.CHUNK_SIZE_CUBED);
            Array.Clear(visitZP, 0, Chunk.CHUNK_SIZE_CUBED);
        }
    }
}