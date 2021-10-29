using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace HexaEngine.Resources
{
    public class RenderTexture : Resource
    {
        public ID3D11Texture2D RenderTargetTexture { get; private set; }

        public ID3D11ShaderResourceView ShaderResourceView { get; private set; }

        public ID3D11SamplerState SamplerState { get; private set; }

        public SamplerDescription SamplerDescription { get; private set; } = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            ComparisonFunction = ComparisonFunction.Always,
            BorderColor = new Color(0, 0, 0, 0),  // Black Border.
            MinLOD = 0,
            MaxLOD = float.MaxValue
        };

        private ID3D11RenderTargetView RenderTargetView { get; set; }

        public ID3D11Texture2D DepthStencilBuffer { get; set; }

        public ID3D11DepthStencilView DepthStencilView { get; set; }

        public Viewport ViewPort { get; set; }

        public bool Initialize(ID3D11Device device, string name, int width = 1024, int height = 1024)
        {
            try
            {
                // Initialize and set up the render target description.
                Texture2DDescription textureDesc = new()
                {
                    // Shadow Map Texture size as a 1024x1024 Square
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R32G32B32A32_Float,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };

                // Create the render target texture.
                RenderTargetTexture = device.CreateTexture2D(textureDesc);
                RenderTargetTexture.DebugName = name + nameof(RenderTargetTexture);
                // Setup the description of the render target view.
                RenderTargetViewDescription renderTargetViewDesc = new()
                {
                    Format = textureDesc.Format,
                    ViewDimension = RenderTargetViewDimension.Texture2D,
                };
                renderTargetViewDesc.Texture2D.MipSlice = 0;

                // Create the render target view.
                RenderTargetView = device.CreateRenderTargetView(RenderTargetTexture, renderTargetViewDesc);
                RenderTargetView.DebugName = name + nameof(RenderTargetView);

                // Setup the description of the shader resource view.
                ShaderResourceViewDescription shaderResourceViewDesc = new()
                {
                    Format = textureDesc.Format,
                    ViewDimension = ShaderResourceViewDimension.Texture2D,
                };
                shaderResourceViewDesc.Texture2D.MipLevels = 1;
                shaderResourceViewDesc.Texture2D.MostDetailedMip = 0;

                // Create the render target view.
                ShaderResourceView = device.CreateShaderResourceView(RenderTargetTexture, shaderResourceViewDesc);
                ShaderResourceView.DebugName = name + nameof(ShaderResourceView);

                SamplerState = device.CreateSamplerState(SamplerDescription);
                SamplerState.DebugName = name + nameof(SamplerState);

                // Initialize and Set up the description of the depth buffer.
                Texture2DDescription depthStencilDesc = new()
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.D24_UNorm_S8_UInt,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                };

                // Create the texture for the depth buffer using the filled out description.
                DepthStencilBuffer = device.CreateTexture2D(depthStencilDesc);
                DepthStencilBuffer.DebugName = name + nameof(DepthStencilBuffer);

                // Initailze the depth stencil view description.
                DepthStencilViewDescription deothStencilViewDesc = new()
                {
                    Format = Format.D24_UNorm_S8_UInt,
                    ViewDimension = DepthStencilViewDimension.Texture2D
                };
                deothStencilViewDesc.Texture2D.MipSlice = 0;

                // Create the depth stencil view.
                DepthStencilView = device.CreateDepthStencilView(DepthStencilBuffer, deothStencilViewDesc);
                DepthStencilView.DebugName = name + nameof(DepthStencilView);

                // Setup the viewport for rendering.
                ViewPort = new Viewport(0, 0, width, height, 0f, 1f);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetRenderTarget(ID3D11DeviceContext context)
        {
            context.OMSetRenderTargets(RenderTargetView, DepthStencilView);
            context.RSSetViewport(ViewPort);
        }

        public void ClearAndSetRenderTarget(ID3D11DeviceContext context)
        {
            SetRenderTarget(context);
            context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1f, 0);
            context.ClearRenderTargetView(RenderTargetView, new Color4(0, 0, 0, 0));
        }

        public void Render(ID3D11DeviceContext context, int slot = 0)
        {
            context.PSSetShaderResource(slot, ShaderResourceView);
            context.PSSetSampler(slot, SamplerState);
        }

        protected override void Dispose(bool disposing)
        {
            SamplerState?.Dispose();
            SamplerState = null;
            RenderTargetTexture.Dispose();
            RenderTargetTexture = null;
            RenderTargetView.Dispose();
            RenderTargetView = null;
            ShaderResourceView.Dispose();
            ShaderResourceView = null;
            DepthStencilBuffer.Dispose();
            DepthStencilBuffer = null;
            DepthStencilView.Dispose();
            DepthStencilView = null;
            base.Dispose(disposing);
        }
    }
}