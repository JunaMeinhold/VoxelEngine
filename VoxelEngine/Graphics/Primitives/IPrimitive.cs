namespace VoxelEngine.Graphics.Primitives
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;

    public interface IPrimitive : IDisposable
    {
        void DrawAuto(ComPtr<ID3D11DeviceContext> context, GraphicsPipelineState pso);

        void DrawAuto(ComPtr<ID3D11DeviceContext> context);

        void Bind(ComPtr<ID3D11DeviceContext> context, out int vertexCount, out int indexCount, out int instanceCount);

        void Unbind(ComPtr<ID3D11DeviceContext> context);
    }
}