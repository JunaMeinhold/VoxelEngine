namespace VoxelEngine.Graphics.Buffers
{
    using Vortice.Direct3D11;

    public interface IConstantBuffer<T> : IConstantBuffer
    {
        void Resize(ID3D11Device device, int length);

        void Update(ID3D11DeviceContext context, T value);

        void Update(ID3D11DeviceContext context, T[] value);

        unsafe void Update(ID3D11DeviceContext context, T* value, int length);
    }

    public interface IConstantBuffer : IDisposable
    {
        void Update(ID3D11DeviceContext context);
    }
}