namespace VoxelEngine.Rendering.D3D.Shaders
{
    using System.Numerics;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D.Interfaces;

    public interface IShaderLogic : IDisposable
    {
        void Initialize(ID3D11Device device, out ShaderDescription description);

        void Update(ID3D11DeviceContext context, IView view, Matrix4x4 transform);
    }
}