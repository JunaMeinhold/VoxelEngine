using HexaEngine.Windows;
using System;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Color = System.Drawing.Color;

namespace HexaEngine.Shaders
{
    public class GBuffers : IDisposable
    {
        // Variables
        private const int BUFFER_COUNT = 4;

        private bool disposedValue;

        // Properties
        private ID3D11Texture2D[] RenderTargetTexture2DArray { get; set; }

        public ID3D11RenderTargetView[] RenderTargetViewArray { get; set; }

        public ID3D11ShaderResourceView[] ShaderResourceViewArray { get; private set; }

        public ID3D11Texture2D DepthStencilBuffer { get; set; }

        public ID3D11DepthStencilView DepthStencilView { get; set; }

        public ID3D11SamplerState SamplerState { get; set; }

        private Viewport ViewPort { get; set; }

        // Constructor
        public GBuffers()
        {
            // Initialize Arrays to size.
            RenderTargetTexture2DArray = new ID3D11Texture2D[BUFFER_COUNT];
            RenderTargetViewArray = new ID3D11RenderTargetView[BUFFER_COUNT];
            ShaderResourceViewArray = new ID3D11ShaderResourceView[BUFFER_COUNT];
        }

        // Puvlix Methods
        public bool Initialize(ID3D11Device device, int textureWidth, int textureHeight)
        {
            // Initialize the render target texture description.
            Texture2DDescription textureDesc = new()
            {
                Width = textureWidth,
                Height = textureHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(DeviceManager.Current.MSAASampleCount, DeviceManager.Current.MSAASampleQuality),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.GenerateMips
            };

            // Create the render target textures.
            for (var i = 0; i < BUFFER_COUNT; i++)
            {
                RenderTargetTexture2DArray[i] = device.CreateTexture2D(textureDesc);
                RenderTargetTexture2DArray[i].DebugName = nameof(GBuffers) + "RenderTargetTexture2D";
            }

            // Initialize and setup the render target view
            RenderTargetViewDescription renderTargetViewDesc = new()
            {
                Format = textureDesc.Format,
                ViewDimension = RenderTargetViewDimension.Texture2DMultisampled,
            };
            renderTargetViewDesc.Texture2D.MipSlice = 0;

            // Create the render target view.
            for (var i = 0; i < BUFFER_COUNT; i++)
            {
                RenderTargetViewArray[i] = device.CreateRenderTargetView(RenderTargetTexture2DArray[i], renderTargetViewDesc);
                RenderTargetViewArray[i].DebugName = nameof(GBuffers) + "RenderTargetView";
            }

            // Initialize and setup the shader resource view
            ShaderResourceViewDescription shaderResourceViewDesc = new()
            {
                Format = textureDesc.Format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled,
            };
            shaderResourceViewDesc.Texture2D.MipLevels = 1;
            shaderResourceViewDesc.Texture2D.MostDetailedMip = 0;

            // Create the render target view.
            for (var i = 0; i < BUFFER_COUNT; i++)
            {
                ShaderResourceViewArray[i] = device.CreateShaderResourceView(RenderTargetTexture2DArray[i], shaderResourceViewDesc);
                ShaderResourceViewArray[i].DebugName = nameof(GBuffers) + "ShaderResourceView";
            }

            SamplerDescription samplerDesc = new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MipLODBias = 0,
                MaxAnisotropy = 16,
                ComparisonFunction = ComparisonFunction.Always,
                BorderColor = (Color4)Color.FromArgb(0, 0, 0, 0),  // Black Border.
                MinLOD = 0,
                MaxLOD = float.MaxValue
            };

            SamplerState = device.CreateSamplerState(samplerDesc);
            SamplerState.DebugName = nameof(GBuffers) + nameof(SamplerState);

            // Initialize the description of the depth buffer.
            var depthBufferDesc = new Texture2DDescription()
            {
                Width = textureWidth,
                Height = textureHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(DeviceManager.Current.MSAASampleCount, DeviceManager.Current.MSAASampleQuality),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            // Create the texture for the depth buffer using the filled out description.
            DepthStencilBuffer = device.CreateTexture2D(depthBufferDesc);
            DepthStencilBuffer.DebugName = nameof(GBuffers) + nameof(DepthStencilBuffer);

            // Set up the depth stencil view description.
            var depthStencilViewBufferDesc = new DepthStencilViewDescription()
            {
                Format = Format.D24_UNorm_S8_UInt,
                ViewDimension = DepthStencilViewDimension.Texture2DMultisampled,
                Texture2D = new Texture2DDepthStencilView() { MipSlice = 0 }
            };

            // Create the depth stencil view.
            DepthStencilView = device.CreateDepthStencilView(DepthStencilBuffer, depthStencilViewBufferDesc);
            DepthStencilView.DebugName = nameof(GBuffers) + nameof(DepthStencilView);

            // Setup the viewport for rendering.
            ViewPort = new Viewport(0, 0, textureWidth, textureHeight, 0, 1);

            return true;
        }

        public void ClearAndSetRenderTargets(ID3D11DeviceContext deviceContext)
        {
            // Bind the render target view array and depth stencil buffer to the output render pipeline.
            deviceContext.OMSetRenderTargets(RenderTargetViewArray, DepthStencilView);

            // Clear the render target buffers.
            foreach (var renderTargetView in RenderTargetViewArray)
                deviceContext.ClearRenderTargetView(renderTargetView, Color.Transparent);

            // Clear the depth buffer.
            deviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            // Set the viewport.
            deviceContext.RSSetViewport(ViewPort);
        }

        public void Render(ID3D11DeviceContext context)
        {
            context.PSSetShaderResources(0, ShaderResourceViewArray);
            context.PSSetSampler(0, SamplerState);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                SamplerState?.Dispose();
                SamplerState = null;
                DepthStencilView?.Dispose();
                DepthStencilView = null;
                DepthStencilBuffer?.Dispose();
                DepthStencilBuffer = null;
                for (var i = 0; i < BUFFER_COUNT; i++)
                {
                    ShaderResourceViewArray[i]?.Dispose();
                    ShaderResourceViewArray[i] = null;
                }
                ShaderResourceViewArray = null;
                for (var i = 0; i < BUFFER_COUNT; i++)
                {
                    RenderTargetViewArray[i]?.Dispose();
                    RenderTargetViewArray[i] = null;
                }
                RenderTargetViewArray = null;
                for (var i = 0; i < BUFFER_COUNT; i++)
                {
                    RenderTargetTexture2DArray[i]?.Dispose();
                    RenderTargetTexture2DArray[i] = null;
                }
                RenderTargetTexture2DArray = null;
                disposedValue = true;
            }
        }

        ~GBuffers()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}