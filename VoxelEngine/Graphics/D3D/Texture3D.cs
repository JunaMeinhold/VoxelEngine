namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.IO;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Resources;

    public unsafe class Texture3D : Resource, IShaderResource
    {
        private readonly string dbgName;
        private Texture3DDescription description;
        private Format format;
        private int width;
        private int height;
        private int mipLevels;
        private int arraySize;
        private CpuAccessFlags cpuAccessFlags;
        private GpuAccessFlags gpuAccessFlags;
        private ResourceOptionFlags miscFlag;
        private bool canWrite;
        private bool canRead;

        private bool isDirty;
        private bool disposedValue;
        private int rowPitch;
        private int slicePitch;
        private byte* local;

        private ID3D11Texture3D texture;
        private ID3D11UnorderedAccessView uav;
        private ID3D11ShaderResourceView srv;
        private ID3D11RenderTargetView rtv;

        public Texture3D()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(ID3D11Device device, string path)
        {
            ID3D11Texture3D image = TextureHelper.LoadTexture3DFile(device, Paths.CurrentTexturePath + path);
            texture = image;
            texture.DebugName = nameof(Texture3D);
            srv = device.CreateShaderResourceView(texture);
            srv.DebugName = nameof(Texture3D) + "." + nameof(srv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(ID3D11Device device, Format format, int width, int height, int arraySize = 1, int mipLevels = 0, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceOptionFlags miscFlags = ResourceOptionFlags.None)
        {
            Texture3DDescription description = new(format, width, height, arraySize, mipLevels, BindFlags.None, ResourceUsage.Default, cpuAccessFlags, miscFlags);
            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0 && (gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot read at the same time");
            }

            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0 && (gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot write at the same time");
            }

            if (cpuAccessFlags != CpuAccessFlags.None && (gpuAccessFlags & GpuAccessFlags.UA) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot use rw with uva at the same time");
            }

            if ((gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                description.Usage = ResourceUsage.Default;
                description.BindFlags |= BindFlags.ShaderResource;
            }

            if ((gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                description.Usage = ResourceUsage.Default;
                description.BindFlags |= BindFlags.RenderTarget;
            }

            if ((gpuAccessFlags & GpuAccessFlags.UA) != 0)
            {
                description.Usage = ResourceUsage.Default;
                description.BindFlags |= BindFlags.UnorderedAccess;
            }

            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                description.Usage = ResourceUsage.Dynamic;
                description.BindFlags = BindFlags.ShaderResource;
            }

            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                description.Usage = ResourceUsage.Staging;
                description.BindFlags = BindFlags.None;
            }

            ID3D11Texture3D image = device.CreateTexture3D(description);
            texture = image;
            texture.DebugName = nameof(Texture3D);

            if ((description.BindFlags & BindFlags.UnorderedAccess) != 0)
            {
                uav = device.CreateUnorderedAccessView(texture, new(texture, arraySize > 1 ? UnorderedAccessViewDimension.Texture2DArray : UnorderedAccessViewDimension.Texture2D));
                uav.DebugName = nameof(Texture3D) + ".UAV";
            }

            if ((description.BindFlags & BindFlags.ShaderResource) != 0)
            {
                srv = device.CreateShaderResourceView(texture);
                srv.DebugName = nameof(Texture3D) + ".SRV";
            }

            if ((description.BindFlags & BindFlags.RenderTarget) != 0)
            {
                rtv = device.CreateRenderTargetView(texture);
                rtv.DebugName = nameof(Texture3D) + ".RTV";
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(ID3D11Device device, Texture3DDescription description, SubresourceData initialData)
        {
            ID3D11Texture3D image = device.CreateTexture3D(description, new SubresourceData[] { initialData });
            texture = image;
            texture.DebugName = nameof(Texture3D);
            srv = device.CreateShaderResourceView(texture);
            srv.DebugName = nameof(Texture3D) + "." + nameof(srv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(ID3D11Device device, Texture3DDescription description, SubresourceData[] initialData)
        {
            ID3D11Texture3D image = device.CreateTexture3D(description, initialData);
            texture = image;
            texture.DebugName = nameof(Texture3D);
            srv = device.CreateShaderResourceView(texture);
            srv.DebugName = nameof(Texture3D) + "." + nameof(srv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Load(ID3D11Device device, string path)
        {
            ID3D11Texture3D image = TextureHelper.LoadTexture3DFile(device, Paths.CurrentTexturePath + path);
            texture = image;
            texture.DebugName = nameof(Texture3D);
            srv = device.CreateShaderResourceView(texture);
            srv.DebugName = nameof(Texture3D) + "." + nameof(srv);
        }

        public ID3D11ShaderResourceView SRV => srv;

        public ID3D11RenderTargetView RTV => rtv;

        public ID3D11UnorderedAccessView UAV => uav;

        public ID3D11SamplerState Sampler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context, int slot, ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    context.VSSetShaderResource(slot, srv);
                    context.VSSetSampler(slot, Sampler);
                    break;

                case ShaderStage.Hull:
                    context.HSSetShaderResource(slot, srv);
                    context.HSSetSampler(slot, Sampler);
                    break;

                case ShaderStage.Domain:
                    context.DSSetShaderResource(slot, srv);
                    context.DSSetSampler(slot, Sampler);
                    break;

                case ShaderStage.Pixel:
                    context.PSSetShaderResource(slot, srv);
                    context.PSSetSampler(slot, Sampler);
                    break;
            }
        }

        public void Resize(ID3D11Device device, Format format, int width, int height, int arraySize, int mipLevels, CpuAccessFlags cpuAccessFlags, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceOptionFlags miscFlag = ResourceOptionFlags.None)
        {
            this.format = format;
            this.width = width;
            this.height = height;
            this.mipLevels = mipLevels;
            this.arraySize = arraySize;
            this.cpuAccessFlags = cpuAccessFlags;
            this.gpuAccessFlags = gpuAccessFlags;
            this.miscFlag = miscFlag;
            description = new(format, width, height, arraySize, mipLevels, BindFlags.ShaderResource | BindFlags.RenderTarget, ResourceUsage.Default, cpuAccessFlags, miscFlag);

            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0 && (gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot read at the same time");
            }

            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0 && (gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot write at the same time");
            }

            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                description.Usage = ResourceUsage.Dynamic;
                description.BindFlags = BindFlags.ShaderResource;
                canWrite = true;
            }

            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                description.Usage = ResourceUsage.Staging;
                description.BindFlags = BindFlags.None;
                canRead = true;
            }

            if (cpuAccessFlags != CpuAccessFlags.None)
            {
                local = (byte*)Alloc(rowPitch * height);
                ZeroMemory(local, rowPitch * height);
            }
            texture.Dispose();
            srv?.Dispose();
            rtv?.Dispose();
            uav?.Dispose();

            texture = device.CreateTexture3D(description);
            texture.DebugName = dbgName;

            if ((description.BindFlags & BindFlags.UnorderedAccess) != 0)
            {
                uav = device.CreateUnorderedAccessView(texture, new(texture, arraySize > 1 ? UnorderedAccessViewDimension.Texture2DArray : UnorderedAccessViewDimension.Texture2D));
                uav.DebugName = dbgName + ".UAV";
            }

            if ((description.BindFlags & BindFlags.ShaderResource) != 0)
            {
                srv = device.CreateShaderResourceView(texture);
                srv.DebugName = dbgName + ".SRV";
            }

            if ((description.BindFlags & BindFlags.RenderTarget) != 0)
            {
                rtv = device.CreateRenderTargetView(texture);
                rtv.DebugName = dbgName + ".RTV";
            }
        }

        protected override void Dispose(bool disposing)
        {
            rtv?.Dispose();
            rtv = null;
            uav?.Dispose();
            uav = null;
            srv?.Dispose();
            srv = null;
            texture.Dispose();
            texture = null;
            Sampler?.Dispose();
        }
    }
}