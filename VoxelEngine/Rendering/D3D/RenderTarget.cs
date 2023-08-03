namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Resources;

    public class RenderTarget : Resource, IRenderTarget
    {
        public readonly ID3D11RenderTargetView RTV;
        public DepthStencil DepthStencil;

        public Color4 ClearColor;
        public float X;
        public float Y;
        public float Width;
        public float Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTarget(ID3D11RenderTargetView view, float width, float height)
        {
            RTV = view;
            Width = width;
            Height = height;
            Viewport = new(width, height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTarget(ID3D11Device device, ID3D11Resource resource, float width, float height)
        {
            RTV = device.CreateRenderTargetView(resource);
            RTV.DebugName = nameof(RenderTarget);
            Width = width;
            Height = height;
            Viewport = new(width, height);
        }

        public string DebugName { get => RTV.DebugName; set => RTV.DebugName = value; }

        public Viewport Viewport { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTarget(ID3D11DeviceContext context)
        {
            context.OMSetRenderTargets(RTV, DepthStencil?.DSV);
            context.RSSetViewport(X, Y, Width, Height, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearTarget(ID3D11DeviceContext context)
        {
            context.ClearRenderTargetView(RTV, ClearColor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAndSetTarget(ID3D11DeviceContext context)
        {
            ClearTarget(context);
            SetTarget(context);
        }

        protected override void Dispose(bool disposing)
        {
            RTV.Dispose();
            DepthStencil?.Dispose();
            DepthStencil = null;
        }
    }
}