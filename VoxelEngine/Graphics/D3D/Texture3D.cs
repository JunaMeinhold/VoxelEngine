namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using VoxelEngine.Graphics.D3D.Interfaces;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.IO;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Resources;

    public interface IShaderResourceView : IDeviceChild
    {
    }

    public interface IResource : IDeviceChild
    {
    }

    public interface ITexture3D : IDeviceChild
    {
    }

    public interface IGraphicsDevice : IDeviceChild
    {
        IShaderResourceView CreateShaderResourceView(IResource resource);

        ITexture3D CreateTexture3D(Texture3DDesc description);
    }

    public unsafe class Texture3D : Resource, IShaderResource
    {
        private readonly string dbgName;
        private Texture3DDesc description;
        private Format format;
        private int width;
        private int height;
        private int mipLevels;
        private int arraySize;
        private CpuAccessFlag cpuAccessFlags;
        private GpuAccessFlags gpuAccessFlags;
        private ResourceMiscFlag miscFlag;
        private bool canWrite;
        private bool canRead;

        private bool isDirty;
        private bool disposedValue;
        private int rowPitch;
        private int slicePitch;
        private byte* local;

        private ITexture3D texture;
        private ID3D11UnorderedAccessView uav;
        private ID3D11ShaderResourceView srv;
        private ID3D11RenderTargetView rtv;

        public Texture3D()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(IGraphicsDevice device, string path)
        {
            ITexture3D image = TextureHelper.LoadTexture3DFile(device, Paths.CurrentTexturePath + path);
            texture = image;
            texture.DebugName = nameof(Texture3D);
            srv = device.CreateShaderResourceView(texture);
            srv.DebugName = nameof(Texture3D) + "." + nameof(srv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(IGraphicsDevice device, Format format, int width, int height, int arraySize = 1, int mipLevels = 0, CpuAccessFlag cpuAccessFlags = CpuAccessFlag.None, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.None, ResourceMiscFlag miscFlags = ResourceMiscFlag.None)
        {
            Texture3DDesc description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, Usage.Default, (uint)BindFlag.None, (uint)cpuAccessFlags, (uint)miscFlags);
            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0 && (gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot read at the same time");
            }

            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0 && (gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot write at the same time");
            }

            if (cpuAccessFlags != CpuAccessFlag.None && (gpuAccessFlags & GpuAccessFlags.UA) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot use rw with uva at the same time");
            }

            if ((gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                description.Usage = Usage.Default;
                description.BindFlags |= (uint)BindFlag.ShaderResource;
            }

            if ((gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                description.Usage = Usage.Default;
                description.BindFlags |= (uint)BindFlag.RenderTarget;
            }

            if ((gpuAccessFlags & GpuAccessFlags.UA) != 0)
            {
                description.Usage = Usage.Default;
                description.BindFlags |= (uint)BindFlag.UnorderedAccess;
            }

            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0)
            {
                description.Usage = Usage.Dynamic;
                description.BindFlags = (uint)BindFlag.ShaderResource;
            }

            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0)
            {
                description.Usage = Usage.Staging;
                description.BindFlags = (uint)BindFlag.None;
            }

            ITexture3D image = device.CreateTexture3D(description);
            texture = image;
            texture.DebugName = nameof(Texture3D);

            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                uav = device.CreateUnorderedAccessView(texture, new(texture, arraySize > 1 ? UnorderedAccessViewDimension.Texture2DArray : UnorderedAccessViewDimension.Texture2D));
                uav.DebugName = nameof(Texture3D) + ".UAV";
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                srv = device.CreateShaderResourceView(texture);
                srv.DebugName = nameof(Texture3D) + ".SRV";
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
            {
                rtv = device.CreateRenderTargetView(texture);
                rtv.DebugName = nameof(Texture3D) + ".RTV";
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(ID3D11Device device, Texture3DDesc description, SubresourceData initialData)
        {
            ID3D11Texture3D image = device.CreateTexture3D(description, new SubresourceData[] { initialData });
            texture = image;
            texture.DebugName = nameof(Texture3D);
            srv = device.CreateShaderResourceView(texture);
            srv.DebugName = nameof(Texture3D) + "." + nameof(srv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture3D(ID3D11Device device, Texture3DDesc description, SubresourceData[] initialData)
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

        public void Resize(ID3D11Device device, Format format, int width, int height, int arraySize, int mipLevels, CpuAccessFlag cpuAccessFlags, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.Read, ResourceMiscFlag miscFlag = ResourceMiscFlag.None)
        {
            this.format = format;
            this.width = width;
            this.height = height;
            this.mipLevels = mipLevels;
            this.arraySize = arraySize;
            this.cpuAccessFlags = cpuAccessFlags;
            this.gpuAccessFlags = gpuAccessFlags;
            this.miscFlag = miscFlag;
            description = new((uint)width, (uint)height, (uint)arraySize, (uint)mipLevels, format, Usage.Default, (uint)BindFlag.ShaderResource | (uint)BindFlag.RenderTarget, (uint)cpuAccessFlags, (uint)miscFlag);

            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0 && (gpuAccessFlags & GpuAccessFlags.Read) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot read at the same time");
            }

            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0 && (gpuAccessFlags & GpuAccessFlags.Write) != 0)
            {
                throw new ArgumentException("Cpu and Gpu cannot write at the same time");
            }

            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0)
            {
                description.Usage = Usage.Dynamic;
                description.BindFlags = (uint)BindFlag.ShaderResource;
                canWrite = true;
            }

            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0)
            {
                description.Usage = Usage.Staging;
                description.BindFlags = (uint)BindFlag.None;
                canRead = true;
            }

            if (cpuAccessFlags != CpuAccessFlag.None)
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

            if ((description.BindFlags & (uint)BindFlag.UnorderedAccess) != 0)
            {
                uav = device.CreateUnorderedAccessView(texture, new(texture, arraySize > 1 ? UnorderedAccessViewDimension.Texture2DArray : UnorderedAccessViewDimension.Texture2D));
                uav.DebugName = dbgName + ".UAV";
            }

            if ((description.BindFlags & (uint)BindFlag.ShaderResource) != 0)
            {
                srv = device.CreateShaderResourceView(texture);
                srv.DebugName = dbgName + ".SRV";
            }

            if ((description.BindFlags & (uint)BindFlag.RenderTarget) != 0)
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