namespace App.Pipelines.Effects
{
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Rendering.Shaders;

    public class FXAAEffect
    {
        private readonly GraphicsPipeline pipeline;
        private readonly ID3D11SamplerState samplerState;

        public FXAAEffect(ID3D11Device device)
        {
            pipeline = new(device, new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "fxaa/ps.hlsl",
            }, GraphicsPipelineState.DefaultFullscreen);
            samplerState = device.CreateSamplerState(SamplerDescription.LinearClamp);
            pipeline.SamplerStates.Add(samplerState, ShaderStage.Pixel, 0);
        }

        public ID3D11ShaderResourceView Input { set => pipeline.ShaderResourceViews.SetOrAdd(value, ShaderStage.Pixel, 0); }

        public void Pass(ID3D11DeviceContext context)
        {
            pipeline.Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            pipeline.End(context);
        }

        public void Dispose()
        {
            pipeline.Dispose();
            samplerState.Dispose();
        }
    }
}