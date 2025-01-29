namespace App.Pipelines.Effects
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;

    public class FXAAEffect
    {
        private readonly GraphicsPipelineState pso;
        private readonly SamplerState samplerState;

        public FXAAEffect()
        {
            pso = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "fxaa/ps.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);
            samplerState = new(SamplerDescription.LinearClamp);
            pso.Bindings.SetSampler("g_samLinear", samplerState);
        }

        public IShaderResourceView Input { set => pso.Bindings.SetSRV("g_txProcessed", value); }

        public void Pass(ComPtr<ID3D11DeviceContext> context)
        {
            pso.Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            pso.End(context);
        }

        public void Dispose()
        {
            pso.Dispose();
            samplerState.Dispose();
        }
    }
}