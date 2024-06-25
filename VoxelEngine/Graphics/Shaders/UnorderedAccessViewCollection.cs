namespace VoxelEngine.Rendering.Shaders
{
    using Vortice.Direct3D11;

    public class UnorderedAccessViewCollection : BindingCollection<ID3D11UnorderedAccessView>
    {
        public override void Bind(ID3D11DeviceContext context)
        {
            if (cs is not null)
                context.CSSetUnorderedAccessViews(csStart, cs);
        }

        public override void Unbind(ID3D11DeviceContext context)
        {
            if (cs is not null)
                context.CSUnsetUnorderedAccessViews(csStart, cs.Length);
        }
    }
}