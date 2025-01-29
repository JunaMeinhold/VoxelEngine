namespace App.Pipelines.Forward
{
    using VoxelEngine.Graphics.D3D11;

    public class SkyboxPipeline : RenderPass
    {
        protected override GraphicsPipelineState CreatePipelineState()
        {
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/skybox/vs.hlsl",
                PixelShader = "forward/skybox/preethamSky.hlsl"
            }, new GraphicsPipelineStateDesc()
            {
                Rasterizer = RasterizerDescription.CullNone,
                DepthStencil = DepthStencilDescription.DepthRead
            });
        }
    }
}