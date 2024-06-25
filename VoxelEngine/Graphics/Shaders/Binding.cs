namespace VoxelEngine.Rendering.Shaders
{
    using VoxelEngine.Graphics.Shaders;

    public struct Binding<T>
    {
        public ShaderStage Stage;
        public int Slot;
        public T Value;

        public Binding(ShaderStage stage, int slot, T value)
        {
            Stage = stage;
            Slot = slot;
            Value = value;
        }
    }
}