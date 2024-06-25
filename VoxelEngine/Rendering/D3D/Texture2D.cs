namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.IO;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Resources;

    public class Texture2D : Resource, IShaderResource
    {
        private readonly List<ShaderResourceBinding> bindings = new();
        private ID3D11Texture2D texture;
        private ID3D11ShaderResourceView resourceView;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Load(ID3D11Device device, string path)
        {
            ID3D11Texture2D image = TextureHelper.LoadFromFile(device, Paths.CurrentTexturePath + path);
            texture = image;
            texture.DebugName = nameof(Texture2D);
            resourceView = device.CreateShaderResourceView(texture);
            resourceView.DebugName = nameof(Texture2D) + "." + nameof(resourceView);
        }

        public ID3D11SamplerState Sampler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context, int slot, ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    context.VSSetShaderResource(slot, resourceView);
                    context.VSSetSampler(slot, Sampler);
                    break;

                case ShaderStage.Hull:
                    context.HSSetShaderResource(slot, resourceView);
                    context.HSSetSampler(slot, Sampler);
                    break;

                case ShaderStage.Domain:
                    context.DSSetShaderResource(slot, resourceView);
                    context.DSSetSampler(slot, Sampler);
                    break;

                case ShaderStage.Pixel:
                    context.PSSetShaderResource(slot, resourceView);
                    context.PSSetSampler(slot, Sampler);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context)
        {
            foreach (ShaderResourceBinding binding in bindings)
            {
                Bind(context, binding.Slot, binding.Stage);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ShaderResourceBinding binding)
        {
            bindings.Add(binding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ShaderResourceBinding binding)
        {
            bindings.Remove(binding);
        }

        protected override void Dispose(bool disposing)
        {
            resourceView.Dispose();
            resourceView = null;
            texture.Dispose();
            texture = null;
            Sampler?.Dispose();
        }
    }
}