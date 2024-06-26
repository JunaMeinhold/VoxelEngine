namespace VoxelEngine.Rendering.D3D
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D.Interfaces;

    public unsafe class RenderTarget : DeviceChild, IRenderTargetView
    {
        public ComPtr<ID3D11RenderTargetView1> RTV;
        public DepthStencil? DepthStencil;

        public Vector4 ClearColor;

        public float Width;
        public float Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTarget(ComPtr<ID3D11RenderTargetView1> view, float width, float height) : base(view.Handle)
        {
            RTV = view;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RenderTarget(ComPtr<ID3D11Device5> device, ComPtr<ID3D11Resource> resource, float width, float height)
        {
            ID3D11RenderTargetView1* rtv;
            device.CreateRenderTargetView1(resource.Handle, (RenderTargetViewDesc1*)null, &rtv);
            nativePointer = (nint)rtv;
            RTV = new(rtv);
            DebugName = nameof(RenderTarget);
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTarget(IDeviceContext context)
        {
            context.SetRenderTarget(this, DepthStencil);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearTarget(IDeviceContext context, Vector4 color)
        {
            context.ClearRenderTargetView(this, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearTarget(IDeviceContext context)
        {
            Vector4 color = ClearColor;
            context.ClearRenderTargetView(this, ClearColor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAndSetTarget(IDeviceContext context)
        {
            ClearTarget(context);
            SetTarget(context);
        }
    }
}