namespace VoxelEngine.Rendering.DXGI
{
    using System;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using VoxelEngine.Rendering.D3D;

    public unsafe class DXGISwapChain : IDisposable
    {
        private readonly ComPtr<ID3D11Device> device;
        private readonly ComPtr<IDXGISwapChain1> swapChain;
        private readonly SwapChainDesc1 description;
        private RenderTarget renderTarget;
        private bool disposedValue;

        public DXGISwapChain(ComPtr<ID3D11Device> device, ComPtr<IDXGISwapChain1> swapChain, SwapChainDesc1 description)
        {
            this.device = device;
            this.swapChain = swapChain;
            this.description = description;
            InitializeRenderTargets();
        }

        public RenderTarget RenderTarget => renderTarget;

        public DepthStencil DepthStencil { get => renderTarget.DepthStencil; set => renderTarget.DepthStencil = value; }

        private void InitializeRenderTargets()
        {
            ComPtr<ID3D11Texture2D1> buffer = swapChain.GetBuffer<ID3D11Texture2D1>(0);
            Texture2DDesc desc;
            buffer.GetDesc(&desc);
            renderTarget = new(device, buffer, desc.Width, desc.Height);
            buffer.Dispose();
        }

        public void Present(uint sync)
        {
            swapChain.Present(sync, 0);
        }

        public void ResizeBuffers(uint bufferCount, uint width, uint height, Format format, SwapChainFlag flags)
        {
            renderTarget.Dispose();
            swapChain.ResizeBuffers(bufferCount, width, height, format, (uint)flags);
            InitializeRenderTargets();
        }

        public void Resize(uint width, uint height)
        {
            renderTarget.Dispose();
            swapChain.ResizeBuffers(description.BufferCount, width, height, description.Format, description.Flags);
            InitializeRenderTargets();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                renderTarget?.Dispose();
                swapChain.Dispose();
                disposedValue = true;
            }
        }

        ~DXGISwapChain()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SetTarget(ComPtr<ID3D11DeviceContext> context)
        {
            renderTarget.SetTarget(context);
        }

        public void ClearAndSetTarget(ComPtr<ID3D11DeviceContext> context)
        {
            renderTarget.ClearAndSetTarget(context);
        }
    }
}