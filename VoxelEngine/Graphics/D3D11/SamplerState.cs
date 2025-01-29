namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Resources;

    public unsafe class SamplerState : DisposableRefBase, ISamplerState
    {
        private ComPtr<ID3D11SamplerState> sampler;

        public SamplerState(SamplerDesc desc)
        {
            var device = D3D11DeviceManager.Device;
            device.CreateSamplerState(&desc, out sampler);
        }

        public ComPtr<ID3D11SamplerState> Sampler => sampler;

        public nint NativePointer => (nint)sampler.Handle;

        public static implicit operator ComPtr<ID3D11SamplerState>(SamplerState sampler) => sampler.sampler;

        protected override void DisposeCore()
        {
            if (sampler.Handle != null)
            {
                sampler.Release();
                sampler = default;
            }
        }
    }
}