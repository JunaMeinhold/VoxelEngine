namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using Newtonsoft.Json.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public unsafe class ConstantBuffer<T> : Resource, IConstantBuffer<T> where T : unmanaged
    {
        private BufferDesc description;
        public ComPtr<ID3D11Buffer> Buffer;
        private readonly bool isDynamic;

        private T* data;
        private int count;

        public ConstantBuffer(CpuAccessFlag accessFlags, T* value, int count)
        {
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlag.Write => Usage.Dynamic,
                CpuAccessFlag.Read => Usage.Staging,
                0 => Usage.Immutable,
                _ => throw new NotImplementedException(),
            };

            description = new((uint)(sizeof(T) * count), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            if (accessFlags != 0)
            {
                data = AllocCopyT(value, count);
                this.count = count;
            }

            var subresourceData = new SubresourceData(value);
            device.CreateBuffer(ref description, ref subresourceData, out Buffer);

            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(CpuAccessFlag accessFlags, T value)
        {
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlag.Write => Usage.Dynamic,
                CpuAccessFlag.Read => Usage.Staging,
                0 => Usage.Immutable,
                _ => throw new NotImplementedException(),
            };

            description = new((uint)sizeof(T), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            if (accessFlags != 0)
            {
                data = AllocT<T>(); ZeroMemoryT(data);
                count = 1;
            }
            var subresourceData = new SubresourceData(&value);
            device.CreateBuffer(ref description, ref subresourceData, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(CpuAccessFlag accessFlags, int count)
        {
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlag.Write => Usage.Dynamic,
                CpuAccessFlag.Read => Usage.Staging,
                0 => throw new NotSupportedException("Immutable buffers need initial data"),
                _ => throw new NotImplementedException(),
            };

            description = new((uint)(sizeof(T) * count), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            data = AllocT<T>(count); ZeroMemoryT(data, count);
            this.count = count;
            device.CreateBuffer(ref description, null, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(CpuAccessFlag accessFlags)
        {
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlag.Write => Usage.Dynamic,
                CpuAccessFlag.Read => Usage.Staging,
                0 => throw new NotSupportedException("Immutable buffers need initial data"),
                _ => throw new NotImplementedException(),
            };

            description = new((uint)sizeof(T), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            data = AllocT<T>(); ZeroMemoryT(data);
            count = 1;
            device.CreateBuffer(ref description, null, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public nint NativePointer => (nint)Buffer.Handle;

        public void Update(ComPtr<ID3D11DeviceContext> context)
        {
            DeviceHelper.Write(context, Buffer, data, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ComPtr<ID3D11DeviceContext> context, T value)
        {
            DeviceHelper.Write(context, Buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Update(ComPtr<ID3D11DeviceContext> context, T* value, int length)
        {
            DeviceHelper.Write(context, Buffer, value, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ComPtr<ID3D11DeviceContext> context, T[] value)
        {
            DeviceHelper.Write(context, Buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int length)
        {
            var device = D3D11DeviceManager.Device;
            if (Buffer.Handle != null)
            {
                Buffer.Release();
                Buffer = null;
            }
            description.ByteWidth = (uint)(sizeof(T) * length);
            device.CreateBuffer(ref description, null, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public static implicit operator ComPtr<ID3D11Buffer>(ConstantBuffer<T> value)
        {
            return value.Buffer;
        }

        protected override void DisposeCore()
        {
            if (Buffer.Handle != null)
            {
                Buffer.Release();
                Buffer = null;
            }

            if (data != null)
            {
                Free(data);
                data = null;
                count = 0;
            }
        }
    }
}