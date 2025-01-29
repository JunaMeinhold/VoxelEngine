namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;

    public interface IConstantBuffer<T> : IConstantBuffer where T : unmanaged
    {
        void Resize(int length);

        void Update(ComPtr<ID3D11DeviceContext> context, T value);

        void Update(ComPtr<ID3D11DeviceContext> context, T[] value);

        unsafe void Update(ComPtr<ID3D11DeviceContext> context, T* value, int length);
    }

    public interface IConstantBuffer : IBuffer, IDisposable
    {
        void Update(ComPtr<ID3D11DeviceContext> context);
    }
}