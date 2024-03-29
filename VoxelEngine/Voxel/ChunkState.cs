﻿namespace VoxelEngine.Voxel
{
    public enum ChunkState
    {
        /// <summary>
        /// Chunk does not exist and must be generated by the ChunkGen.
        /// </summary>
        None,

        /// <summary>
        /// No impact
        /// </summary>
        OnDisk,

        /// <summary>
        /// Medium memory impact (only vertex buffer)
        /// </summary>
        OnGpu,

        /// <summary>
        /// Highest memory impact (full data)
        /// </summary>
        OnCpu
    }
}