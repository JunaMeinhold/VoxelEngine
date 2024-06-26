namespace VoxelEngine.Graphics
{
    using Silk.NET.Direct3D11;
    using VoxelEngine.Graphics.D3D.Interfaces;

    public unsafe class DeviceChild : DisposableBase, IDeviceChild
    {
        protected nint nativePointer;

        public DeviceChild(nint ptr)
        {
            nativePointer = ptr;
        }

        public DeviceChild(void* ptr)
        {
            nativePointer = (nint)ptr;
        }

        public DeviceChild()
        {
        }

        public string? DebugName { get => GetDebugName((void*)nativePointer); set => SetDebugName((void*)nativePointer, value); }

        public nint NativePointer => nativePointer;

        protected override void DisposeCore()
        {
            ((ID3D11DeviceChild*)nativePointer)->Release();
        }
    }
}