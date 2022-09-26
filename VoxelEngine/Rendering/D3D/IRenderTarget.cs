namespace VoxelEngine.Rendering.D3D
{
    using Vortice.Direct3D11;

    public interface IRenderTarget
    {
        void ClearAndSetTarget(ID3D11DeviceContext context);

        void ClearTarget(ID3D11DeviceContext context);

        void SetTarget(ID3D11DeviceContext context);
    }
}