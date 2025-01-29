namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.IO;

    public unsafe class Texture3D : DisposableRefBase, IShaderResourceView, IUnorderedAccessView, IRenderTargetView
    {
        private readonly string dbgName;
        private Texture3DDesc description;
        private GpuAccessFlags gpuAccessFlags;

        private bool isDirty;
        private bool disposedValue;
        private int rowPitch;
        private int slicePitch;
        private byte* local;

        private ComPtr<ID3D11Texture3D> texture;
        private ComPtr<ID3D11UnorderedAccessView> uav;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private ComPtr<ID3D11RenderTargetView> rtv;
        private ComPtr<ID3D11SamplerState> sampler;

        public Texture3D()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(string path, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read)
        {
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);
            texture = TextureHelper.LoadTexture3DFile(device, Paths.CurrentTexturePath + path, description);
            texture.GetDesc(ref description);
            //texture.DebugName = nameof(Texture3D);
            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(Format format, int width, int height, int arraySize = 1, int mipLevels = 0, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, 0, (uint)Usage.Default, (uint)cpuAccessFlag, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            device.CreateTexture3D(ref description, null, out texture);
            //texture.DebugName = nameof(Texture3D);

            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(SubresourceData subresourceData, Format format, int width, int height, int arraySize = 1, int mipLevels = 0, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, 0, (uint)Usage.Default, (uint)cpuAccessFlag, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            device.CreateTexture3D(ref description, &subresourceData, out texture);
            //texture.DebugName = nameof(Texture3D);

            CreateViews(device, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(SubresourceData[] subresourceData, Format format, int width, int height, int arraySize = 1, int mipLevels = 0, CpuAccessFlag cpuAccessFlag = 0, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            Texture3DDesc description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, 0, (uint)Usage.Default, (uint)cpuAccessFlag, (uint)miscFlags);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            device.CreateTexture3D(ref description, ref subresourceData[0], out texture);
            //texture.DebugName = nameof(Texture3D);

            CreateViews(device, description);
        }

        public ComPtr<ID3D11ShaderResourceView> SRV => srv;

        public ComPtr<ID3D11RenderTargetView> RTV => rtv;

        public ComPtr<ID3D11UnorderedAccessView> UAV => uav;

        public ComPtr<ID3D11SamplerState> Sampler
        {
            get => sampler;
            set
            {
                if (sampler.Handle != null)
                {
                    sampler.Release();
                }
                sampler = value;
                if (value.Handle != null)
                {
                    value.AddRef();
                }
            }
        }

        nint IShaderResourceView.NativePointer => (nint)srv.Handle;

        nint IRenderTargetView.NativePointer => (nint)rtv.Handle;

        nint IUnorderedAccessView.NativePointer => (nint)uav.Handle;

        public nint NativePointer => (nint)texture.Handle;

        public void Resize(Format format, int width, int height, int arraySize, int mipLevels, CpuAccessFlag cpuAccessFlag, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceMiscFlag miscFlag = 0)
        {
            this.gpuAccessFlags = gpuAccessFlags;
            ComPtr<ID3D11Device> device = D3D11DeviceManager.Device.As<ID3D11Device>();
            description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, Usage.Default, 0, (uint)cpuAccessFlag, (uint)miscFlag);
            (description.Usage, description.BindFlags) = TextureHelper.ConvertToUB(cpuAccessFlag, gpuAccessFlags);

            if (cpuAccessFlag != 0)
            {
                local = (byte*)Alloc(rowPitch * height);
                ZeroMemory(local, rowPitch * height);
            }
            DisposeCore();

            device.CreateTexture3D(ref description, null, out texture).ThrowIf();
            //texture.DebugName = dbgName;

            CreateViews(device, description);
        }

        private void CreateViews(ComPtr<ID3D11Device> device, Texture3DDesc description)
        {
            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                device.CreateUnorderedAccessView(texture.As<ID3D11Resource>(), null, out uav);
                //uav.DebugName = nameof(Texture2D) + ".UAV";
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out srv);
                //srv.DebugName = nameof(Texture2D) + ".SRV";
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
            {
                device.CreateRenderTargetView(texture.As<ID3D11Resource>(), null, out rtv);
                //rtv.DebugName = nameof(Texture2D) + ".RTV";
            }
        }

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
            if (sampler.Handle != null)
            {
                sampler.Release();
                sampler = null;
            }
        }
    }
}