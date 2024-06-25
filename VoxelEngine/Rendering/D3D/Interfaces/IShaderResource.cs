namespace VoxelEngine.Rendering.D3D.Interfaces
{
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D.Shaders;

    public interface IShaderResource : IDisposable
    {
        void Bind(ID3D11DeviceContext context);

        void Add(ShaderResourceBinding binding);

        void Remove(ShaderResourceBinding binding);
    }
}