﻿namespace VoxelEngine.Graphics.D3D11
{
    public struct ComputePipelineDesc
    {
        public string Shader;
        public string ShaderEntry = "main";
        public ShaderMacro[]? Macros;

        public ComputePipelineDesc()
        {
        }
    }
}