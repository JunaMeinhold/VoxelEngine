namespace App.Pipelines.Forward
{
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.Shaders;

    public class LinePipeline : GraphicsPipeline
    {
        public LinePipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "forward/line/vertex.hlsl",
            PixelShader = "forward/line/pixel.hlsl",
        }, new GraphicsPipelineState()
        {
            Topology = PrimitiveTopology.LineList
        })
        {
        }
    }
}