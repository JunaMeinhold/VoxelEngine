namespace App.Pipelines.Forward
{
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.D3D11;

    public class LinePipeline : GraphicsPipeline
    {
        public LinePipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "forward/line/vertex.hlsl",
            PixelShader = "forward/line/pixel.hlsl",
        }, new GraphicsPipelineStateDesc()
        {
            Topology = PrimitiveTopology.LineList
        })
        {
        }
    }
}