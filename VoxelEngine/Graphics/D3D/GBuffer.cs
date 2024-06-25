namespace VoxelEngine.Rendering.D3D
{
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Resources;

    public class GBuffer : Resource, IShaderResource
    {
        private ID3D11Texture2D[] textures;
        private ID3D11ShaderResourceView[] resourceViews;
        private Format[] formats;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Count { get; private set; }

        public Format[] Formats => formats;

        public RenderTargetArray RenderTargets { get; private set; }

        public GBuffer(ID3D11Device device, int width, int height, params Format[] formats)
        {
            Count = formats.Length;
            Width = width;
            Height = height;
            textures = new ID3D11Texture2D[formats.Length];
            resourceViews = new ID3D11ShaderResourceView[formats.Length];
            this.formats = formats;
            for (int i = 0; i < formats.Length; i++)
            {
                Format format = formats[i];
                Texture2DDescription textureDesc = new()
                {
                    Width = Width,
                    Height = Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };

                ID3D11Texture2D texture = device.CreateTexture2D(textureDesc);
                ID3D11ShaderResourceView resourceView = device.CreateShaderResourceView(texture);

                textures[i] = texture;
                resourceViews[i] = resourceView;
            }

            RenderTargets = new(device, textures, width, height);
        }

        public ID3D11ShaderResourceView[] SRVs => resourceViews;

        public void Resize(ID3D11Device device, int width, int height, params Format[] formats)
        {
            if (formats == null || formats.Length == 0)
            {
                formats = this.formats;
            }

            Width = width;
            Height = height;
            this.formats = formats;
            Count = formats.Length;

            for (int i = 0; i < formats.Length; i++)
            {
                Format format = formats[i];
                Texture2DDescription textureDesc = new()
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };

                ID3D11Texture2D texture = device.CreateTexture2D(textureDesc);
                ID3D11ShaderResourceView resourceView = device.CreateShaderResourceView(texture);

                textures[i] = texture;
                resourceViews[i] = resourceView;
            }

            RenderTargets = new(device, textures, width, height);
        }

        public static implicit operator ID3D11ShaderResourceView[](GBuffer array)
        {
            return array.resourceViews;
        }

        protected override void Dispose(bool disposing)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                ID3D11Texture2D texture = textures[i];
                texture.Dispose();
            }

            for (int i = 0; i < resourceViews.Length; i++)
            {
                ID3D11ShaderResourceView view = resourceViews[i];
                view.Dispose();
            }

            textures = null;
            resourceViews = null;
            formats = null;
            RenderTargets.Dispose();
        }
    }
}