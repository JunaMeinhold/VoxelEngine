namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Resources;

    public unsafe class GBuffer : Resource
    {
        private ComPtr<ID3D11Texture2D>[] textures;
        private ComPtr<ID3D11ShaderResourceView>[] srvs;
        private ComPtr<ID3D11RenderTargetView>[] rtvs;
        private Format[] formats;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Count { get; private set; }

        public Format[] Formats => formats;

        public GBuffer(int width, int height, params Format[] formats)
        {
            var device = D3D11DeviceManager.Device;
            Count = formats.Length;
            Width = width;
            Height = height;
            textures = new ComPtr<ID3D11Texture2D>[formats.Length];
            srvs = new ComPtr<ID3D11ShaderResourceView>[formats.Length];
            rtvs = new ComPtr<ID3D11RenderTargetView>[formats.Length];
            this.formats = formats;
            for (int i = 0; i < formats.Length; i++)
            {
                Format format = formats[i];
                Texture2DDesc textureDesc = new()
                {
                    Width = (uint)Width,
                    Height = (uint)Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                    BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
                    CPUAccessFlags = 0,
                    MiscFlags = 0
                };

                device.CreateTexture2D(ref textureDesc, null, out ComPtr<ID3D11Texture2D> texture);
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11ShaderResourceView> srv);
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11RenderTargetView> rtv);

                textures[i] = texture;
                srvs[i] = srv;
                rtvs[i] = rtv;
            }
        }

        public ComPtr<ID3D11ShaderResourceView>[] SRVs => srvs;

        public ComPtr<ID3D11RenderTargetView>[] RTVs => rtvs;

        public void Resize(int width, int height, params Format[] formats)
        {
            var device = D3D11DeviceManager.Device;
            if (formats == null || formats.Length == 0)
            {
                formats = this.formats;
            }

            Width = width;
            Height = height;
            this.formats = formats;
            Count = formats.Length;

            for (int i = 0; i < textures.Length; i++)
            {
                srvs[i].Release();
                rtvs[i].Release();
                textures[i].Release();
            }

            textures = new ComPtr<ID3D11Texture2D>[formats.Length];
            srvs = new ComPtr<ID3D11ShaderResourceView>[formats.Length];
            rtvs = new ComPtr<ID3D11RenderTargetView>[formats.Length];

            for (int i = 0; i < formats.Length; i++)
            {
                Format format = formats[i];
                Texture2DDesc textureDesc = new()
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                    BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
                    CPUAccessFlags = 0,
                    MiscFlags = 0
                };

                device.CreateTexture2D(ref textureDesc, null, out ComPtr<ID3D11Texture2D> texture);
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11ShaderResourceView> srv);
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out ComPtr<ID3D11RenderTargetView> rtv);

                textures[i] = texture;
                srvs[i] = srv;
                rtvs[i] = rtv;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTarget(ComPtr<ID3D11DeviceContext> context, DepthStencil? depthStencil)
        {
            ID3D11RenderTargetView** ppRtv = stackalloc ID3D11RenderTargetView*[D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];

            for (int i = 0; i < rtvs.Length; i++)
            {
                ppRtv[i] = rtvs[i].Handle;
            }

            context.OMSetRenderTargets((uint)rtvs.Length, ppRtv, depthStencil?.DSV ?? null);
        }

        public void ClearTarget(ComPtr<ID3D11DeviceContext> context, Vector4 color)
        {
            float* pColor = (float*)&color;
            for (int i = 0; i < rtvs.Length; i++)
            {
                context.ClearRenderTargetView(rtvs[i], pColor);
            }
        }

        public static implicit operator ComPtr<ID3D11ShaderResourceView>[](GBuffer array)
        {
            return array.srvs;
        }

        public static implicit operator ComPtr<ID3D11RenderTargetView>[](GBuffer array)
        {
            return array.rtvs;
        }

        protected override void DisposeCore()
        {
            for (int i = 0; i < textures.Length; i++)
            {
                srvs[i].Release();
                rtvs[i].Release();
                textures[i].Release();
            }

            textures = null;
            srvs = null;
            formats = null;
        }
    }
}