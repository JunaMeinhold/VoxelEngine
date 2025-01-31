namespace VoxelEngine.Graphics.D3D11
{
    public abstract class RenderPass : DisposableRefBase
    {
        protected GraphicsPipelineState state;

        public RenderPass()
        {
            state = CreatePipelineState();
        }

        public D3D11ResourceBindingList Bindings => state.Bindings;

        protected abstract GraphicsPipelineState CreatePipelineState();

        public void Begin(GraphicsContext context)
        {
            context.SetGraphicsPipelineState(state);
        }

        public void End(GraphicsContext context)
        {
            context.SetGraphicsPipelineState(null);
        }

        protected override void DisposeCore()
        {
            state.Dispose();
        }
    }
}