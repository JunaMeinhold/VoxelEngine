namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Resources;
    using Format = Hexa.NET.DXGI.Format;

    public unsafe class DepthStencil : Resource
    {
        private ComPtr<ID3D11Texture2D> texture;

        private ComPtr<ID3D11DepthStencilView> depthStencilView;
        private ComPtr<ID3D11ShaderResourceView> shaderResourceView;

        private Format format;
        private int width;
        private int height;
        private int arraySize;

        public Viewport Viewport => new(width, height);

        public DepthStencil(int width, int height) : this(Format.D32Float, width, height, 1)
        {
        }

        public DepthStencil(int width, int height, int arraySize) : this(Format.D32Float, width, height, arraySize)
        {
        }

        public DepthStencil(Format format, int width, int height, int arraySize)
        {
            var device = D3D11DeviceManager.Device;
            this.format = format;
            this.width = width;
            this.height = height;
            this.arraySize = arraySize;
            Texture2DDesc depthBufferDesc;
            depthBufferDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = (uint)arraySize,
                Format = GetDepthResourceFormat(format),
                SampleDesc = new(1, 0),
                Usage = Usage.Default,
                BindFlags = (uint)(BindFlag.DepthStencil | BindFlag.ShaderResource),
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            device.CreateTexture2D(ref depthBufferDesc, null, out texture);
            //texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            DepthStencilViewDesc dsvdesc = new(format, arraySize > 1 ? DsvDimension.Texture2Darray : DsvDimension.Texture2D);
            device.CreateDepthStencilView(texture.As<ID3D11Resource>(), ref dsvdesc, out depthStencilView);
            //depthStencilView.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            ShaderResourceViewDesc srvdesc = new(GetDepthSRVFormat(format), arraySize > 1 ? SrvDimension.Texture2Darray : SrvDimension.Texture2D);
            device.CreateShaderResourceView(texture.As<ID3D11Resource>(), ref srvdesc, out shaderResourceView);
            //shaderResourceView.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        public ComPtr<ID3D11DepthStencilView> DSV => depthStencilView;

        public ComPtr<ID3D11ShaderResourceView> SRV => shaderResourceView;

        private static Format GetDepthResourceFormat(Format depthFormat)
        {
            Format resformat = Format.Unknown;
            switch (depthFormat)
            {
                case Format.D16Unorm:
                    resformat = Format.R32Typeless;
                    break;

                case Format.D24UnormS8Uint:
                    resformat = Format.R24G8Typeless;
                    break;

                case Format.D32Float:
                    resformat = Format.R32Typeless;
                    break;

                case Format.D32FloatS8X24Uint:
                    resformat = Format.R32G8X24Typeless;
                    break;
            }

            return resformat;
        }

        private static Format GetDepthSRVFormat(Format depthFormat)
        {
            Format srvformat = Format.Unknown;
            switch (depthFormat)
            {
                case Format.D16Unorm:
                    srvformat = Format.R16Float;
                    break;

                case Format.D24UnormS8Uint:
                    srvformat = Format.R24UnormX8Typeless;
                    break;

                case Format.D32Float:
                    srvformat = Format.R32Float;
                    break;

                case Format.D32FloatS8X24Uint:
                    srvformat = Format.R32FloatX8X24Typeless;
                    break;
            }
            return srvformat;
        }

        public void Resize(int width, int height)
        {
            Resize(width, height, arraySize, format);
        }

        public void Resize(int width, int height, int arraySize)
        {
            Resize(width, height, arraySize, format);
        }

        public void Resize(int width, int height, int arraySize, Format format)
        {
            var device = D3D11DeviceManager.Device;
            this.format = format;
            this.width = width;
            this.height = height;
            this.arraySize = arraySize;

            texture.Dispose();
            depthStencilView.Dispose();
            shaderResourceView.Dispose();

            Texture2DDesc depthBufferDesc;
            depthBufferDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = (uint)arraySize,
                Format = GetDepthResourceFormat(format),
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
                BindFlags = (uint)(BindFlag.DepthStencil | BindFlag.ShaderResource),
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            device.CreateTexture2D(ref depthBufferDesc, null, out texture);
            //texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            DepthStencilViewDesc dsvdesc = new(format, arraySize > 1 ? DsvDimension.Texture2Darray : DsvDimension.Texture2D);
            device.CreateDepthStencilView(texture.As<ID3D11Resource>(), ref dsvdesc, out depthStencilView);
            //depthStencilView.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            ShaderResourceViewDesc srvdesc = new(GetDepthSRVFormat(format), arraySize > 1 ? SrvDimension.Texture2Darray : SrvDimension.Texture2D);
            device.CreateShaderResourceView(texture.As<ID3D11Resource>(), ref srvdesc, out shaderResourceView);
            //shaderResourceView.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        public static implicit operator ComPtr<ID3D11ShaderResourceView>(DepthStencil texture) => texture.shaderResourceView;

        public static implicit operator ComPtr<ID3D11DepthStencilView>(DepthStencil texture) => texture.depthStencilView;

        public static implicit operator ComPtr<ID3D11Texture2D>(DepthStencil texture) => texture.texture;

        protected override void DisposeCore()
        {
            DSV.Dispose();
            SRV.Dispose();
            texture.Dispose();
        }
    }
}