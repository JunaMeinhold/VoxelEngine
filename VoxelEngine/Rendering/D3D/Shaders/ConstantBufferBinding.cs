namespace VoxelEngine.Rendering.D3D.Shaders
{
    public struct ConstantBufferBinding
    {
        public ConstantBufferBinding(ShaderStage stage, int slot)
        {
            Stage = stage;
            Slot = slot;
        }

        public ShaderStage Stage { get; set; }

        public int Slot { get; set; }
    }
}