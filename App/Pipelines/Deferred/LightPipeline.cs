namespace App.Pipelines.Deferred
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;

    public class DeferredLightPass : RenderPass
    {
        private readonly ConstantBuffer<CBDirectionalLightSD> directionalLightBuffer;

        public DeferredLightPass()
        {
            directionalLightBuffer = new(CpuAccessFlag.Write);
            state.Bindings.SetCBV("directionalLightBuffer", directionalLightBuffer);
        }

        protected override GraphicsPipelineState CreatePipelineState()
        {
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "deferred/light/ps.hlsl",
            }, new()
            {
                Blend = BlendDescription.Additive,
                DepthStencil = DepthStencilDescription.None,
                Topology = PrimitiveTopology.Trianglestrip
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ComPtr<ID3D11DeviceContext> context, CBDirectionalLightSD light)
        {
            directionalLightBuffer.Update(context, light);
        }

        public void Pass(ComPtr<ID3D11DeviceContext> context)
        {
            Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            End(context);
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            directionalLightBuffer.Dispose();
        }
    }
}