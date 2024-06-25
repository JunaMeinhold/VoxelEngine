namespace VoxelEngine.Graphics.Primitives
{
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.Shaders;

    public interface IPrimitive : IDisposable
    {
        void DrawAuto(ID3D11DeviceContext context, GraphicsPipeline pipeline);

        void DrawAuto(ID3D11DeviceContext context);

        void Bind(ID3D11DeviceContext context, out int vertexCount, out int indexCount, out int instanceCount);

        void Unbind(ID3D11DeviceContext context);
    }
}