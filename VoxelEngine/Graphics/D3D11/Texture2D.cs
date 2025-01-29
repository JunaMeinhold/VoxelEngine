namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.IO;

    public unsafe class Texture2D : DisposableRefBase, IShaderResourceView, IUnorderedAccessView, IRenderTargetView
    {
        private readonly string dbgName;
        private Texture2DDesc description;
        private GpuAccessFlags gpuAccessFlags;
        private bool canWrite;
        private bool canRead;

        private bool isDirty;
        private bool disposedValue;
        private int rowPitch;
        private int slicePitch;
        private byte* local;

        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11UnorderedAccessView> uav;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private ComPtr<ID3D11RenderTargetView> rtv;

        public Texture2D(string[] paths, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            texture = TextureHelper.LoadFromFiles(device, paths);
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(string path, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);
            texture = TextureHelper.LoadTexture2DFile(device, Paths.CurrentTexturePath + path, description);
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(Format format, int width, int height, int arraySize = 1, int mipLevels = 1, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlag, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            device.CreateTexture2D(ref description, null, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(SubresourceData subresourceData, Format format, int width, int height, int arraySize = 1, int mipLevels = 1, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlag, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            device.CreateTexture2D(ref description, &subresourceData, out texture).ThrowIf();
            texture.GetDesc(ref description);
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D(SubresourceData[] subresourceData, Format format, int width, int height, int arraySize = 1, int mipLevels = 1, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"{file}, {line}";
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture2DDesc description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlag, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            device.CreateTexture2D(ref description, ref subresourceData[0], out texture).ThrowIf();
            Utils.SetDebugName(texture, $"{dbgName}.{nameof(Texture2D)}");
            CreateViews(device, description);
        }

        public ComPtr<ID3D11ShaderResourceView> SRV => srv;

        public ComPtr<ID3D11RenderTargetView> RTV => rtv;

        public ComPtr<ID3D11UnorderedAccessView> UAV => uav;

        nint IShaderResourceView.NativePointer => (nint)srv.Handle;

        nint IRenderTargetView.NativePointer => (nint)rtv.Handle;

        nint IUnorderedAccessView.NativePointer => (nint)uav.Handle;

        nint IDeviceChild.NativePointer => (nint)texture.Handle;

        public Hexa.NET.Mathematics.Viewport Viewport => new(description.Width, description.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ComPtr<ID3D11DeviceContext> context, int slot, ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    context.VSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;

                case ShaderStage.Hull:
                    context.HSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;

                case ShaderStage.Domain:
                    context.DSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;

                case ShaderStage.Pixel:
                    context.PSSetShaderResources((uint)slot, 1, srv.GetAddressOf());
                    break;
            }
        }

        public void Resize(int width, int height)
        {
            Resize(description.Format, width, height, (int)description.ArraySize, (int)description.MipLevels, (CpuAccessFlag)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(int width, int height, int arraySize, int mipLevels)
        {
            Resize(description.Format, width, height, arraySize, mipLevels, (CpuAccessFlag)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(Format format, int width, int height, int arraySize, int mipLevels)
        {
            Resize(format, width, height, arraySize, mipLevels, (CpuAccessFlag)description.CPUAccessFlags, gpuAccessFlags, (ResourceMiscFlag)description.MiscFlags);
        }

        public void Resize(Format format, int width, int height, int arraySize, int mipLevels, CpuAccessFlag cpuAccessFlag, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceMiscFlag miscFlag = 0)
        {
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, new SampleDesc(1, 0), 0, 0, (uint)cpuAccessFlag, (uint)miscFlag);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);
            CreateViews(device, description);

            if (cpuAccessFlag != 0)
            {
                local = (byte*)Alloc(rowPitch * height);
                ZeroMemory(local, rowPitch * height);
            }

            DisposeCore();

            device.CreateTexture2D(ref description, null, out texture).ThrowIf();
            //texture.DebugName = dbgName;
            CreateViews(device, description);
        }

        private void CreateViews(ComPtr<ID3D11Device> device, Texture2DDesc description)
        {
            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                UnorderedAccessViewDesc desc = new(description.Format, description.ArraySize > 1 ? UavDimension.Texture2Darray : UavDimension.Texture2D);
                device.CreateUnorderedAccessView(texture.As<ID3D11Resource>(), ref desc, out uav).ThrowIf();
                //uav.DebugName = nameof(Texture2D) + ".UAV";
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out srv).ThrowIf();
                //srv.DebugName = nameof(Texture2D) + ".SRV";
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
            {
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out rtv).ThrowIf();
                //rtv.DebugName = nameof(Texture2D) + ".RTV";
            }
        }

        public MappedSubresource Map(ComPtr<ID3D11DeviceContext> context, uint subresource, Map map, MapFlag flag = 0)
        {
            MappedSubresource mapped;
            context.Map(texture.As<ID3D11Resource>(), subresource, map, (uint)flag, &mapped);
            return mapped;
        }

        public void Unmap(ComPtr<ID3D11DeviceContext> context, uint subresource)
        {
            context.Unmap(texture.As<ID3D11Resource>(), subresource);
        }

        public static implicit operator ComPtr<ID3D11ShaderResourceView>(Texture2D texture) => texture.SRV;

        public static implicit operator ComPtr<ID3D11RenderTargetView>(Texture2D texture) => texture.RTV;

        public static implicit operator ComPtr<ID3D11Texture2D>(Texture2D texture) => texture.texture;

        protected override void DisposeCore()
        {
            if (texture.Handle != null)
            {
                texture.Dispose();
                texture = default;
            }
            if (srv.Handle != null)
            {
                srv.Dispose();
                srv = default;
            }
            if (rtv.Handle != null)
            {
                rtv.Dispose();
                rtv = default;
            }
            if (uav.Handle != null)
            {
                uav.Dispose();
                uav = default;
            }
        }
    }
}