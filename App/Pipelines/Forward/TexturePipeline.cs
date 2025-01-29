namespace App.Pipelines.Forward
{
    using VoxelEngine.Graphics.D3D11;

    public class TexturePipeline : RenderPass
    {
        protected override GraphicsPipelineState CreatePipelineState()
        {
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/texture/vs.hlsl",
                PixelShader = "forward/texture/ps.hlsl",
            }, new GraphicsPipelineStateDesc()
            {
                DepthStencil = DepthStencilDescription.None,
                Blend = BlendDescription.AlphaBlend,
            });
        }
    }
}