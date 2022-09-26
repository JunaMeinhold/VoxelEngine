namespace VoxelEngine.Rendering.D3D.Interfaces
{
    using Vortice.Direct3D11;

    public interface IConstantBuffer<T> : IConstantBuffer
    {
        void Resize(ID3D11Device device, int length);

        void Write(ID3D11DeviceContext context, T value);

        void Write(ID3D11DeviceContext context, T[] value);
    }

    public interface IConstantBuffer : IDisposable
    {
        void Bind(ID3D11DeviceContext context);
    }
}