namespace VoxelEngine.Rendering.D3D.Shaders
{
    public struct ShaderResourceBinding : IEquatable<ShaderResourceBinding>
    {
        public ShaderResourceBinding(ShaderStage stage, int slot)
        {
            Stage = stage;
            Slot = slot;
        }

        public ShaderStage Stage { get; set; }

        public int Slot { get; set; }

        public bool Equals(ShaderResourceBinding other)
        {
            return Stage == other.Stage && Slot == other.Slot;
        }

        public override bool Equals(object obj)
        {
            return obj is ShaderResourceBinding binding && Equals(binding);
        }

        public static bool operator ==(ShaderResourceBinding left, ShaderResourceBinding right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShaderResourceBinding left, ShaderResourceBinding right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return Slot.GetHashCode() + Stage.GetHashCode();
        }
    }
}