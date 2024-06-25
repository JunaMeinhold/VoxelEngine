namespace VoxelEngine.Rendering.Shaders
{
    using Vortice.Direct3D11;

    public class ConstantBufferCollection : BindingCollection<ID3D11Buffer>
    {
        public override void Bind(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSSetConstantBuffers(vsStart, vs);
            if (hs is not null)
                context.HSSetConstantBuffers(hsStart, hs);
            if (ds is not null)
                context.DSSetConstantBuffers(dsStart, ds);
            if (gs is not null)
                context.GSSetConstantBuffers(gsStart, gs);
            if (ps is not null)
                context.PSSetConstantBuffers(psStart, ps);
            if (cs is not null)
                context.CSSetConstantBuffers(csStart, cs);
        }

        public override void Unbind(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSUnsetConstantBuffers(vsStart, vs.Length);
            if (hs is not null)
                context.HSUnsetConstantBuffers(hsStart, hs.Length);
            if (ds is not null)
                context.DSUnsetConstantBuffers(dsStart, ds.Length);
            if (gs is not null)
                context.GSUnsetConstantBuffers(gsStart, gs.Length);
            if (ps is not null)
                context.PSUnsetConstantBuffers(psStart, ps.Length);
            if (cs is not null)
                context.CSUnsetConstantBuffers(csStart, cs.Length);
        }
    }
}