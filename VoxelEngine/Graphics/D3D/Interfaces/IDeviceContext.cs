namespace VoxelEngine.Graphics.D3D.Interfaces
{
    using System.Numerics;

    public interface IRenderTargetView : IDeviceChild
    {
    }

    public interface IDepthStencilView : IDeviceChild
    {
    }

    public interface IDeviceContext : IDeviceChild
    {
        void ClearRenderTargetView(IRenderTargetView rtv, Vector4 color);

        void SetRenderTarget(IRenderTargetView? rtv, IDepthStencilView? dsv);

        unsafe void SetRenderTargets(uint numRTVs, void** rtv, IDepthStencilView? dsv);
    }
}