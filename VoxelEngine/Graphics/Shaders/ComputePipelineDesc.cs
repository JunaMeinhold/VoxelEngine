namespace VoxelEngine.Rendering.Shaders
{
    public struct ComputePipelineDesc
    {
        public string Shader;
        public string ShaderEntry = "main";

        public ComputePipelineDesc()
        {
        }
    }
}