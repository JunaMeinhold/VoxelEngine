namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Core;
    using VoxelEngine.Resources;
    using Format = Vortice.DXGI.Format;

    public class DepthStencil : Resource
    {
        private ID3D11Texture2D texture;
        public readonly ID3D11DepthStencilView DepthStencilView;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DepthStencil(ID3D11Device device, int width, int heigth, bool msaa = false)
        {
            Texture2DDescription depthBufferDesc;
            if (msaa)
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.D32_Float_S8X24_UInt,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
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
                    Format = Format.D24_UNorm_S8_UInt,
                    SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);
            DepthStencilView = device.CreateDepthStencilView(texture);
            DepthStencilView.DebugName = nameof(DepthStencil) + "." + nameof(DepthStencilView);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DepthStencil(ID3D11Device device, int width, int heigth, int arraySize, bool msaa = false)
        {
            Texture2DDescription depthBufferDesc;
            if (msaa)
            {
                depthBufferDesc = new()
                {
                    Width = width,
                    Height = heigth,
                    MipLevels = 1,
                    ArraySize = arraySize,
                    Format = Format.D32_Float_S8X24_UInt,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
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
                    Format = Format.D24_UNorm_S8_UInt,
                    SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };
            }

            texture = device.CreateTexture2D(depthBufferDesc);
            texture.DebugName = nameof(DepthStencil) + "." + nameof(texture);
            DepthStencilView = device.CreateDepthStencilView(texture);
            DepthStencilView.DebugName = nameof(DepthStencil) + "." + nameof(DepthStencilView);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.None, 1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepth(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearStencil(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Stencil, 1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepthStencil(ID3D11DeviceContext context)
        {
            context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
        }

        protected override void Dispose(bool disposing)
        {
            DepthStencilView.Dispose();
            texture.Dispose();
            texture = null;
        }
    }
}