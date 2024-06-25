namespace VoxelEngine.Rendering.D3D
{
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using VoxelEngine.Resources;
    using Format = Vortice.DXGI.Format;

    public class DepthStencil : Resource
    {
        private ID3D11Texture2D texture;

        private ID3D11DepthStencilView depthStencilView;
        private ID3D11ShaderResourceView shaderResourceView;

        private Format format;
        private int width;
        private int height;
        private int arraySize;

        public Viewport Viewport => new(width, height);

        public DepthStencil(ID3D11Device device, int width, int height) : this(device, Format.D32_Float, width, height, 1)
        {
        }

        public DepthStencil(ID3D11Device device, int width, int height, int arraySize) : this(device, Format.D32_Float, width, height, arraySize)
        {
        }

        public DepthStencil(ID3D11Device device, Format format, int width, int height, int arraySize)
        {
            this.format = format;
            this.width = width;
            this.height = height;
            this.arraySize = arraySize;
            Texture2DDescription depthBufferDesc;
            depthBufferDesc = new()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = arraySize,
                Format = GetDepthResourceFormat(format),
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            var dsvdesc = new DepthStencilViewDescription(texture, arraySize > 1 ? DepthStencilViewDimension.Texture2DArray : DepthStencilViewDimension.Texture2D, format);
            depthStencilView = device.CreateDepthStencilView(texture, dsvdesc);
            depthStencilView.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            var srvdesc = new ShaderResourceViewDescription(texture, arraySize > 1 ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Texture2D, GetDepthSRVFormat(format));
            shaderResourceView = device.CreateShaderResourceView(texture, srvdesc);
            shaderResourceView.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        public ID3D11DepthStencilView DSV => depthStencilView;

        public ID3D11ShaderResourceView SRV => shaderResourceView;

        private static Format GetDepthResourceFormat(Format depthFormat)
        {
            Format resformat = Format.Unknown;
            switch (depthFormat)
            {
                case Format.D16_UNorm:
                    resformat = Format.R32_Typeless;
                    break;

                case Format.D24_UNorm_S8_UInt:
                    resformat = Format.R24G8_Typeless;
                    break;

                case Format.D32_Float:
                    resformat = Format.R32_Typeless;
                    break;

                case Format.D32_Float_S8X24_UInt:
                    resformat = Format.R32G8X24_Typeless;
                    break;
            }

            return resformat;
        }

        private static Format GetDepthSRVFormat(Format depthFormat)
        {
            Format srvformat = Format.Unknown;
            switch (depthFormat)
            {
                case Format.D16_UNorm:
                    srvformat = Format.R16_Float;
                    break;

                case Format.D24_UNorm_S8_UInt:
                    srvformat = Format.R24_UNorm_X8_Typeless;
                    break;

                case Format.D32_Float:
                    srvformat = Format.R32_Float;
                    break;

                case Format.D32_Float_S8X24_UInt:
                    srvformat = Format.R32_Float_X8X24_Typeless;
                    break;
            }
            return srvformat;
        }

        public void Resize(ID3D11Device device, int width, int height)
        {
            Resize(device, width, height, arraySize, format);
        }

        public void Resize(ID3D11Device device, int width, int height, int arraySize)
        {
            Resize(device, width, height, arraySize, format);
        }

        public void Resize(ID3D11Device device, int width, int height, int arraySize, Format format)
        {
            this.format = format;
            this.width = width;
            this.height = height;
            this.arraySize = arraySize;

            texture.Dispose();
            depthStencilView.Dispose();
            shaderResourceView.Dispose();

            Texture2DDescription depthBufferDesc;
            depthBufferDesc = new()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = arraySize,
                Format = GetDepthResourceFormat(format),
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            var dsvdesc = new DepthStencilViewDescription(texture, arraySize > 1 ? DepthStencilViewDimension.Texture2DArray : DepthStencilViewDimension.Texture2D, format);
            depthStencilView = device.CreateDepthStencilView(texture, dsvdesc);
            depthStencilView.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            var srvdesc = new ShaderResourceViewDescription(texture, arraySize > 1 ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Texture2D, GetDepthSRVFormat(format));
            shaderResourceView = device.CreateShaderResourceView(texture, srvdesc);
            shaderResourceView.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        public static implicit operator ID3D11ShaderResourceView(DepthStencil texture) => texture.shaderResourceView;

        public static implicit operator ID3D11DepthStencilView(DepthStencil texture) => texture.depthStencilView;

        public static implicit operator ID3D11Texture2D(DepthStencil texture) => texture.texture;

        protected override void Dispose(bool disposing)
        {
            DSV.Dispose();
            SRV.Dispose();
            texture.Dispose();
        }
    }
}