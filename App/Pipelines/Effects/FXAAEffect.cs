namespace App.Pipelines.Effects
{
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.Shaders;

    public class FXAAEffect : Pipeline
    {
        public FXAAEffect(ID3D11Device device) : base(device, new()
        {
            VertexShader = "fxaa/vs.hlsl",
            PixelShader = "fxaa/ps.hlsl",
            Topology = PrimitiveTopology.TriangleList,
            Rasterizer = RasterizerDescription.CullBack,
            DepthStencil = DepthStencilDescription.Default,
            Blend = BlendDescription.Opaque,
        })
        {
        }
    }
}