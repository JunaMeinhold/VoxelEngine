namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;

    public abstract class RenderPass : DisposableRefBase
    {
        protected GraphicsPipelineState state;

        public RenderPass()
        {
            state = CreatePipelineState();
        }

        protected abstract GraphicsPipelineState CreatePipelineState();

        public void Begin(ComPtr<ID3D11DeviceContext> context)
        {
            state.Begin(context);
        }

        public void End(ComPtr<ID3D11DeviceContext> context)
        {
            state.End(context);
        }

        protected override void DisposeCore()
        {
            state.Dispose();
        }
    }
}