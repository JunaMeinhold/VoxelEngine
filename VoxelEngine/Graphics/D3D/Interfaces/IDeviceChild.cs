namespace VoxelEngine.Graphics.D3D.Interfaces
{
    public interface IDeviceChild : IDisposable
    {
        string? DebugName { get; set; }

        nint NativePointer { get; }
    }
}