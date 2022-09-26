namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Resources;

    public class RenderTarget : Resource, IRenderTarget
    {
        private ID3D11RenderTargetView view;
        public DepthStencil DepthStencil;

        public Color4 ClearColor;
        public float X;
        public float Y;
        public float Width;
        public float Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTarget(ID3D11RenderTargetView view, float width, float height)
        {
            this.view = view;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTarget(ID3D11Device device, ID3D11Resource resource, float width, float height)
        {
            view = device.CreateRenderTargetView(resource);
            view.DebugName = nameof(RenderTarget);
            Width = width;
            Height = height;
        }

        public string DebugName { get => view.DebugName; set => view.DebugName = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTarget(ID3D11DeviceContext context)
        {
            context.OMSetRenderTargets(view, DepthStencil?.DepthStencilView);
            context.RSSetViewport(X, Y, Width, Height, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearTarget(ID3D11DeviceContext context)
        {
            context.ClearRenderTargetView(view, ClearColor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAndSetTarget(ID3D11DeviceContext context)
        {
            ClearTarget(context);
            SetTarget(context);
        }

        protected override void Dispose(bool disposing)
        {
            view.Dispose();
            view = null;
            DepthStencil?.Dispose();
            DepthStencil = null;
        }
    }
}