namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Resources;

    public class ConstantBuffer<T> : Resource, IConstantBuffer<T> where T : unmanaged
    {
        public ID3D11Buffer Buffer;
        private readonly bool isDynamic;
        private readonly ConstantBufferBinding[] bindings;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, ref T value, ShaderStage stage, int index, bool isDynamic)
        {
            this.isDynamic = isDynamic;
            if (isDynamic)
            {
                Buffer = device.CreateBuffer(value, new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            else
            {
                Buffer = device.CreateBuffer(value, new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            bindings = new ConstantBufferBinding[] { new(stage, index) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, ref T value, bool isDynamic, params ConstantBufferBinding[] bindings)
        {
            this.isDynamic = isDynamic;
            if (isDynamic)
            {
                Buffer = device.CreateBuffer(value, new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            else
            {
                Buffer = device.CreateBuffer(value, new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            this.bindings = bindings;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, T[] value, ShaderStage stage, int index, bool isDynamic)
        {
            this.isDynamic = isDynamic;
            if (isDynamic)
            {
                Buffer = device.CreateBuffer((ReadOnlySpan<T>)value.AsSpan(), new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            else
            {
                Buffer = device.CreateBuffer((ReadOnlySpan<T>)value.AsSpan(), new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            bindings = new ConstantBufferBinding[] { new(stage, index) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, T[] value, bool isDynamic, params ConstantBufferBinding[] bindings)
        {
            this.isDynamic = isDynamic;
            if (isDynamic)
            {
                Buffer = device.CreateBuffer((ReadOnlySpan<T>)value.AsSpan(), new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            else
            {
                Buffer = device.CreateBuffer((ReadOnlySpan<T>)value.AsSpan(), new(Marshal.SizeOf<T>(), BindFlags.ConstantBuffer, ResourceUsage.Default));
                Buffer.DebugName = nameof(ConstantBuffer<T>);
            }
            this.bindings = bindings;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, ShaderStage stage, int index)
        {
            isDynamic = true;
            int size = Marshal.SizeOf<T>();
            Buffer = device.CreateBuffer(new(size, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
            bindings = new ConstantBufferBinding[] { new(stage, index) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, ShaderStage stage, int index, int count)
        {
            isDynamic = true;
            int size = Marshal.SizeOf<T>();
            Buffer = device.CreateBuffer(new(size * count, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
            bindings = new ConstantBufferBinding[] { new(stage, index) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, int count)
        {
            isDynamic = true;
            int size = Marshal.SizeOf<T>();
            Buffer = device.CreateBuffer(new(size * count, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConstantBuffer(ID3D11Device device, params ConstantBufferBinding[] bindings)
        {
            this.bindings = bindings;
            isDynamic = true;
            int size = Marshal.SizeOf<T>();
            Buffer = device.CreateBuffer(new(size, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ID3D11DeviceContext context, T value)
        {
            if (isDynamic)
            {
                DeviceHelper.Write(context, Buffer, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ID3D11DeviceContext context, T[] value)
        {
            if (isDynamic)
            {
                DeviceHelper.Write(context, Buffer, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(ID3D11Device device, int length)
        {
            Buffer.Dispose();
            int size = Marshal.SizeOf<T>() * length;
            Buffer = device.CreateBuffer(new(size, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                ConstantBufferBinding binding = bindings[i];
                switch (binding.Stage)
                {
                    case ShaderStage.Vertex:
                        context.VSSetConstantBuffer(binding.Slot, Buffer);
                        break;

                    case ShaderStage.Hull:
                        context.HSSetConstantBuffer(binding.Slot, Buffer);
                        break;

                    case ShaderStage.Domain:
                        context.DSSetConstantBuffer(binding.Slot, Buffer);
                        break;

                    case ShaderStage.Geometry:
                        context.GSSetConstantBuffer(binding.Slot, Buffer);
                        break;

                    case ShaderStage.Pixel:
                        context.PSSetConstantBuffer(binding.Slot, Buffer);
                        break;
                }
            }
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