namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Resources;

    public class RenderTargetArray : Resource, IRenderTarget
    {
        private ID3D11RenderTargetView[] views;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTargetArray(ID3D11RenderTargetView[] views, float width, float height)
        {
            this.views = views;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTargetArray(ID3D11Device device, ID3D11Resource[] resources, float width, float height)
        {
            views = new ID3D11RenderTargetView[resources.Length];
            for (int i = 0; i < resources.Length; i++)
            {
                views[i] = device.CreateRenderTargetView(resources[i]);
                views[i].DebugName = nameof(RenderTargetArray) + "." + i;
            }
            Width = width;
            Height = height;
        }

        public DepthStencil DepthStencil;
        public Color4 ClearColor;

        public float X;
        public float Y;
        public float Width;
        public float Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTarget(ID3D11DeviceContext context)
        {
            context.OMSetRenderTargets(views, DepthStencil?.DSV);
            context.RSSetViewport(X, Y, Width, Height, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public void UnsetTarget(ID3D11DeviceContext context)
        {
            context.OMSetRenderTargets(new ID3D11RenderTargetView[1] { null });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearTarget(ID3D11DeviceContext context)
        {
            foreach (ID3D11RenderTargetView view in views)
            {
                context.ClearRenderTargetView(view, ClearColor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAndSetTarget(ID3D11DeviceContext context)
        {
            ClearTarget(context);
            SetTarget(context);
        }

        protected override void Dispose(bool disposing)
        {
            foreach (ID3D11RenderTargetView view in views)
            {
                view.Dispose();
            }

            views = null;
            DepthStencil?.Dispose();
            DepthStencil = null;
        }
    }
}