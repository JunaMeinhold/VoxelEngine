namespace App.Pipelines.Forward
{
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.D3D11;

    public class TexturePipeline : GraphicsPipeline
    {
        public TexturePipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "forward/texture/vs.hlsl",
            PixelShader = "forward/texture/ps.hlsl",
        }, new GraphicsPipelineStateDesc()
        {
            DepthStencil = DepthStencilDescription.None,
            Blend = BlendDescription.AlphaBlend,
        })
        {
        }
    }
}