namespace VoxelEngine.Rendering.Shaders
{
    using Vortice.Direct3D11;

    public class SamplerStateCollection : BindingCollection<ID3D11SamplerState>
    {
        public override void Bind(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSSetSamplers(vsStart, vs);
            if (hs is not null)
                context.HSSetSamplers(hsStart, hs);
            if (ds is not null)
                context.DSSetSamplers(dsStart, ds);
            if (gs is not null)
                context.GSSetSamplers(gsStart, gs);
            if (ps is not null)
                context.PSSetSamplers(psStart, ps);
            if (cs is not null)
                context.CSSetSamplers(csStart, cs);
        }

        public override void Unbind(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSUnsetSamplers(vsStart, vs.Length);
            if (hs is not null)
                context.HSUnsetSamplers(hsStart, hs.Length);
            if (ds is not null)
                context.DSUnsetSamplers(dsStart, ds.Length);
            if (gs is not null)
                context.GSUnsetSamplers(gsStart, gs.Length);
            if (ps is not null)
                context.PSUnsetSamplers(psStart, ps.Length);
            if (cs is not null)
                context.CSUnsetSamplers(csStart, cs.Length);
        }
    }
}