namespace VoxelEngine.Rendering.Shaders
{
    using Vortice.Direct3D11;

    public class ShaderResourceViewCollection : BindingCollection<ID3D11ShaderResourceView>
    {
        public override void Bind(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSSetShaderResources(vsStart, vs);
            if (hs is not null)
                context.HSSetShaderResources(hsStart, hs);
            if (ds is not null)
                context.DSSetShaderResources(dsStart, ds);
            if (gs is not null)
                context.GSSetShaderResources(gsStart, gs);
            if (ps is not null)
                context.PSSetShaderResources(psStart, ps);
            if (cs is not null)
                context.CSSetShaderResources(csStart, cs);
        }

        public override void Unbind(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSUnsetShaderResources(vsStart, vs.Length);
            if (hs is not null)
                context.HSUnsetShaderResources(hsStart, hs.Length);
            if (ds is not null)
                context.DSUnsetShaderResources(dsStart, ds.Length);
            if (gs is not null)
                context.GSUnsetShaderResources(gsStart, gs.Length);
            if (ps is not null)
                context.PSUnsetShaderResources(psStart, ps.Length);
            if (cs is not null)
                context.CSUnsetShaderResources(csStart, cs.Length);
        }
    }
}