namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Core;
    using VoxelEngine.Resources;
    using Format = Vortice.DXGI.Format;

    public class DepthStencil : Resource
    {
        private ID3D11Texture2D texture;
        public readonly ID3D11DepthStencilView DSV;
        public readonly ID3D11ShaderResourceView SRV;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DepthStencil(ID3D11Device device, int width, int heigth, bool msaa = false)
        {
            Texture2DDescription depthBufferDesc;
            if (!msaa)
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }
            else
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            var dsvdesc = new DepthStencilViewDescription(texture, DepthStencilViewDimension.Texture2D, Format.D32_Float);
            DSV = device.CreateDepthStencilView(texture, dsvdesc);
            DSV.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            var srvdesc = new ShaderResourceViewDescription(texture, ShaderResourceViewDimension.Texture2D);
            srvdesc.Format = Format.R32_Float;
            SRV = device.CreateShaderResourceView(texture, srvdesc);
            SRV.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DepthStencil(ID3D11Device device, int width, int heigth, int arraySize, bool msaa = false)
        {
            Texture2DDescription depthBufferDesc;
            if (!msaa)
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = arraySize,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }
            else
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = arraySize,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            var dsvdesc = new DepthStencilViewDescription(texture, arraySize > 1 ? DepthStencilViewDimension.Texture2DArray : DepthStencilViewDimension.Texture2D, Format.D32_Float);
            DSV = device.CreateDepthStencilView(texture, dsvdesc);
            DSV.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            var srvdesc = new ShaderResourceViewDescription(texture, arraySize > 1 ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Texture2D);
            srvdesc.Format = Format.R32_Float;
            SRV = device.CreateShaderResourceView(texture, srvdesc);
            SRV.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DepthStencil(ID3D11Device device, int width, int heigth, int arraySize, Format format, bool msaa = false)
        {
            Texture2DDescription depthBufferDesc;
            if (!msaa)
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = arraySize,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }
            else
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = arraySize,
                    Format = Format.R32_Typeless,
                    SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);

            var dsvdesc = new DepthStencilViewDescription(texture, arraySize > 1 ? DepthStencilViewDimension.Texture2DArray : DepthStencilViewDimension.Texture2D, Format.D32_Float);
            DSV = device.CreateDepthStencilView(texture, dsvdesc);
            DSV.DebugName = nameof(DepthStencil) + "." + nameof(DSV);

            var srvdesc = new ShaderResourceViewDescription(texture, arraySize > 1 ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Texture2D);
            srvdesc.Format = Format.R32_Float;
            SRV = device.CreateShaderResourceView(texture, srvdesc);
            SRV.DebugName = nameof(DepthStencil) + "." + nameof(SRV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.None, 1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepth(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.Depth, 1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepth(ID3D11DeviceContext context, float depth)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.Depth, depth, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearStencil(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.Stencil, 1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepthStencil(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
        }

        protected override void Dispose(bool disposing)
        {
            DSV.Dispose();
            SRV.Dispose();
            texture.Dispose();
            texture = null;
        }
    }
}