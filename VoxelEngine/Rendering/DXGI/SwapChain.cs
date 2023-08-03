namespace VoxelEngine.Rendering.DXGI
{
    using System;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D;

    public class SwapChain : IDisposable
    {
        private readonly ID3D11Device device;
        private readonly IDXGISwapChain1 swapChain;
        private readonly SwapChainDescription1 description;
        private RenderTarget renderTarget;
        private bool disposedValue;

        public SwapChain(ID3D11Device device, IDXGISwapChain1 swapChain, SwapChainDescription1 description)
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
            ID3D11Texture2D1 buffer = swapChain.GetBuffer<ID3D11Texture2D1>(0);
            renderTarget = new(device, buffer, buffer.Description.Width, buffer.Description.Height);
            buffer.Dispose();
        }

        public void Present(int sync)
        {
            swapChain.Present(sync);
        }

        public void ResizeBuffers(int bufferCount, int width, int height, Format format, SwapChainFlags flags)
        {
            renderTarget.Dispose();
            swapChain.ResizeBuffers(bufferCount, width, height, format, flags);
            InitializeRenderTargets();
        }

        public void Resize(int width, int height)
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

        ~SwapChain()
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

        public void SetTarget(ID3D11DeviceContext context)
        {
            renderTarget.SetTarget(context);
        }

        public void ClearAndSetTarget(ID3D11DeviceContext context)
        {
            renderTarget.ClearAndSetTarget(context);
        }
    }
}