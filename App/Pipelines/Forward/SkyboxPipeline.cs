namespace App.Pipelines.Forward
{
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.D3D11;

    public class SkyboxPipeline : GraphicsPipeline
    {
        public SkyboxPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "forward/skybox/vs.hlsl",
            PixelShader = "forward/skybox/preethamSky.hlsl",
        }, new GraphicsPipelineStateDesc()
        {
            Rasterizer = RasterizerDescription.CullNone,
            DepthStencil = DepthStencilDescription.DepthRead
        })
        {
        }
    }
}