namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Core;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Resources;

    [Obsolete("Use Texture1D Texture2D Texture2DArray and Texture3D instead!")]
    public class RenderTexture : Resource, IShaderResource
    {
        private ID3D11ShaderResourceView resourceView;
        private ID3D11Texture2D texture;

        public readonly RenderTarget RenderTarget;
        public readonly int Width;
        public readonly int Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderTexture(ID3D11Device device, int size = -1, Format format = Format.R8G8B8A8_UNorm, bool depthStencil = false, bool msaa = false) : this(device, size, size, format, depthStencil, msaa)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderTexture(ID3D11Device device, int width, int height, Format format = Format.R8G8B8A8_UNorm, bool depthStencil = false, bool msaa = false)
        {
            Width = width;
            Height = height;
            if (msaa)
            {
                texture = device.CreateTexture2DMultisample(format, Width, Height, Nucleus.Settings.MSAASampleCount, bindFlags: BindFlags.ShaderResource | BindFlags.RenderTarget);
                resourceView = device.CreateShaderResourceView(texture);
            }
            else
            {
                texture = device.CreateTexture2D(format, Width, Height, mipLevels: 1, bindFlags: BindFlags.ShaderResource | BindFlags.RenderTarget);
                resourceView = device.CreateShaderResourceView(texture);
            }

            RenderTarget = new(device, texture, width, height);
            if (depthStencil)
            {
                RenderTarget.DepthStencil = new(device, Width, Height);
            }
        }

        public RenderTexture(ID3D11Device device, int width, int height, int count, Format format = Format.R8G8B8A8_UNorm, bool depthStencil = false)
        {
            Width = width;
            Height = height;

            texture = device.CreateTexture2D(format, Width, Height, count, mipLevels: 1, bindFlags: BindFlags.ShaderResource | BindFlags.RenderTarget);
            resourceView = device.CreateShaderResourceView(texture);

            RenderTarget = new(device, texture, width, height);
            if (depthStencil)
            {
                RenderTarget.DepthStencil = new(device, Width, Height, count);
            }
        }

        public ID3D11ShaderResourceView SRV => resourceView;
        public ID3D11RenderTargetView RTV => RenderTarget.RTV;

        public static implicit operator ID3D11ShaderResourceView(RenderTexture texture)
        {
            return texture.resourceView;
        }

        protected override void Dispose(bool disposing)
        {
            resourceView.Dispose();
            resourceView = null;
            texture.Dispose();
            texture = null;
            RenderTarget.Dispose();
        }
    }
}