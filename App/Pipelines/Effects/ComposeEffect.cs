namespace App.Pipelines.Effects
{
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.Shaders;

    public class ComposeEffect : Pipeline
    {
        public ComposeEffect(ID3D11Device device) : base(device, new()
        {
            VertexShader = "compose/vs.hlsl",
            PixelShader = "compose/ps.hlsl",
            Topology = PrimitiveTopology.TriangleList,
            Rasterizer = RasterizerDescription.CullBack,
            DepthStencil = DepthStencilDescription.Default,
            Blend = BlendDescription.Opaque,
        })
        {
        }
    }
}