namespace VoxelEngine.Graphics.Buffers
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui.Backends.Vulkan;
    using Hexa.NET.Utilities;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public unsafe class IndexBuffer<T> : Resource where T : unmanaged
    {
        private BufferDesc desc;
        private bool isDirty;
        private UnsafeList<T> indices;
        private int indexCount;
        private int indexCapacity;
        private ComPtr<ID3D11Buffer> buffer;
        private readonly Format format;
        private readonly IndexFormat indexFormat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer(CpuAccessFlag cpuAccessFlags, int capacity)
        {
            if (typeof(T) == typeof(uint))
            {
                indexFormat = IndexFormat.UInt32;
            }
            else if (typeof(T) == typeof(ushort))
            {
                indexFormat = IndexFormat.UInt16;
            }
            else
            {
                throw new("Index buffers can only be type of uint or ushort");
            }
            format = typeof(T) == typeof(uint) ? Format.R32Uint : Format.R16Uint;

            var device = D3D11DeviceManager.Device;
            desc = new((uint)(sizeof(T) * capacity), Usage.Default, (uint)BindFlag.IndexBuffer, (uint)cpuAccessFlags);
            indexCapacity = capacity;
            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0)
            {
                desc.Usage = Usage.Dynamic;
            }
            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0)
            {
                desc.Usage = Usage.Staging;
            }
            if (cpuAccessFlags == 0)
            {
                throw new InvalidOperationException("If cpu access flags are none initial data must be provided");
            }

            device.CreateBuffer(ref desc, null, out buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer(CpuAccessFlag cpuAccessFlags, Span<T> indices)
        {
            if (typeof(T) == typeof(uint))
            {
                indexFormat = IndexFormat.UInt32;
            }
            else if (typeof(T) == typeof(ushort))
            {
                indexFormat = IndexFormat.UInt16;
            }
            else
            {
                throw new("Index buffers can only be type of uint or ushort");
            }
            format = typeof(T) == typeof(uint) ? Format.R32Uint : Format.R16Uint;

            var device = D3D11DeviceManager.Device;
            desc = new((uint)(sizeof(T) * indices.Length), Usage.Default, (uint)BindFlag.IndexBuffer, (uint)cpuAccessFlags);
            indexCapacity = indices.Length;
            if ((cpuAccessFlags & CpuAccessFlag.Write) != 0)
            {
                desc.Usage = Usage.Dynamic;
            }
            if ((cpuAccessFlags & CpuAccessFlag.Read) != 0)
            {
                desc.Usage = Usage.Staging;
            }

            fixed (T* pData = indices)
            {
                SubresourceData subresourceData = new(pData);
                device.CreateBuffer(ref desc, ref subresourceData, out buffer);
            }
            indexCount = indices.Length;
        }

        public int Count => indexCount;

        public int IndexCapacity => indexCapacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeBuffers()
        {
            if (indices.Count <= IndexCapacity)
            {
                return;
            }

            if (buffer.Handle != null)
            {
                buffer.Release();
                buffer = null;
            }

            var device = D3D11DeviceManager.Device;

            desc.ByteWidth = (uint)(indices.Count * sizeof(int));
            SubresourceData subresourceData = new(indices.Data);
            device.CreateBuffer(ref desc, ref subresourceData, out buffer);
            //indexBuffer.DebugName = nameof(IndexBuffer);
            indexCapacity = indices.Count;
        }

        private unsafe void UpdateBuffers(ComPtr<ID3D11DeviceContext> context)
        {
            DeviceHelper.Write(context, buffer, indices.Data, indices.Size);
            indexCount = indices.Count;
        }

        public void Append(T vertex)
        {
            indices.Add(vertex);
            isDirty = true;
        }

        public void Append(IEnumerable<T> indicies)
        {
            foreach (var idx in indicies)
            {
                indices.PushBack(idx);
            }

            isDirty = true;
        }

        public void Append(T[] indicies)
        {
            indices.AppendRange(indicies);
            isDirty = true;
        }

        public void Clear()
        {
            indices.Clear();
            isDirty = true;
        }

        public void Bind(ComPtr<ID3D11DeviceContext> context)
        {
            if (isDirty)
            {
                ResizeBuffers();
                UpdateBuffers(context);

                isDirty = false;
            }

            context.IASetIndexBuffer(buffer, format, 0);
        }

        public static implicit operator ComPtr<ID3D11Buffer>(IndexBuffer<T> buffer) => buffer.buffer;

        protected override void DisposeCore()
        {
            if (buffer.Handle != null)
            {
                buffer.Release();
                buffer = null;
            }
        }
    }
}