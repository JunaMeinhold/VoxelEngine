namespace App.Pipelines.Forward
{
    using Hexa.NET.D3DCommon;
    using VoxelEngine.Graphics.D3D11;

    public class LinePipeline : RenderPass
    {
        protected override GraphicsPipelineState CreatePipelineState()
        {
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/line/vs.hlsl",
                PixelShader = "forward/line/ps.hlsl",
            }, new GraphicsPipelineStateDesc()
            {
                Topology = PrimitiveTopology.Linelist
            });
        }
    }
}