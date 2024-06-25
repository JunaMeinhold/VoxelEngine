namespace VoxelEngine.Graphics.Buffers
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Resources;

    public unsafe class ConstantBuffer<T> : Resource, IConstantBuffer<T> where T : unmanaged
    {
        private BufferDescription description;
        public ID3D11Buffer Buffer;
        private readonly bool isDynamic;

        private readonly T* data;

        public ConstantBuffer(ID3D11Device device, CpuAccessFlags accessFlags, T* value, int count)
        {
            ResourceUsage usage = accessFlags switch
            {
                CpuAccessFlags.Write => ResourceUsage.Dynamic,
                CpuAccessFlags.Read => ResourceUsage.Staging,
                CpuAccessFlags.None => ResourceUsage.Immutable,
                _ => throw new NotImplementedException(),
            };

            description = new(sizeof(T) * count, BindFlags.ConstantBuffer, usage, accessFlags);
            if (accessFlags != CpuAccessFlags.None)
            {
                data = AllocCopy(value, count);
            }

            Buffer = device.CreateBuffer(description, new SubresourceData(value));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(ID3D11Device device, CpuAccessFlags accessFlags, T value)
        {
            ResourceUsage usage = accessFlags switch
            {
                CpuAccessFlags.Write => ResourceUsage.Dynamic,
                CpuAccessFlags.Read => ResourceUsage.Staging,
                CpuAccessFlags.None => ResourceUsage.Immutable,
                _ => throw new NotImplementedException(),
            };

            description = new(sizeof(T), BindFlags.ConstantBuffer, usage, accessFlags);
            if (accessFlags != CpuAccessFlags.None)
            {
                data = AllocTAndZero<T>();
            }

            Buffer = device.CreateBuffer(description, new SubresourceData(&value));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(ID3D11Device device, CpuAccessFlags accessFlags, int count)
        {
            ResourceUsage usage = accessFlags switch
            {
                CpuAccessFlags.Write => ResourceUsage.Dynamic,
                CpuAccessFlags.Read => ResourceUsage.Staging,
                CpuAccessFlags.None => throw new NotSupportedException("Immutable buffers need initial data"),
                _ => throw new NotImplementedException(),
            };

            description = new(sizeof(T) * count, BindFlags.ConstantBuffer, usage, accessFlags);
            data = AllocTAndZero<T>(count);
            Buffer = device.CreateBuffer(description);
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(ID3D11Device device, CpuAccessFlags accessFlags)
        {
            ResourceUsage usage = accessFlags switch
            {
                CpuAccessFlags.Write => ResourceUsage.Dynamic,
                CpuAccessFlags.Read => ResourceUsage.Staging,
                CpuAccessFlags.None => throw new NotSupportedException("Immutable buffers need initial data"),
                _ => throw new NotImplementedException(),
            };

            description = new(sizeof(T), BindFlags.ConstantBuffer, usage, accessFlags);
            data = AllocTAndZero<T>();
            Buffer = device.CreateBuffer(description);
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public void Update(ID3D11DeviceContext context)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, T value)
        {
            DeviceHelper.Write(context, Buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Update(ID3D11DeviceContext context, T* value, int length)
        {
            DeviceHelper.Write(context, Buffer, value, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, T[] value)
        {
            DeviceHelper.Write(context, Buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(ID3D11Device device, int length)
        {
            Buffer.Dispose();
            int size = Marshal.SizeOf<T>() * length;
            Buffer = device.CreateBuffer(new(size, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public static implicit operator ID3D11Buffer(ConstantBuffer<T> value)
        {
            return value.Buffer;
        }

        protected override void Dispose(bool disposing)
        {
            Buffer.Dispose();
            Buffer = null;
        }
    }
}