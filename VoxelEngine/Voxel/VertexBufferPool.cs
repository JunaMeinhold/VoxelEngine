namespace VoxelEngine.Voxel
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;

    public unsafe class VertexBufferPool<T> where T : unmanaged
    {
        private readonly ComPtr<ID3D11Device> device;
        private readonly List<ComPtr<ID3D11Buffer>> buffers = [];
        private readonly List<ComPtr<ID3D11Buffer>> freeBuffers = [];
        private readonly SemaphoreSlim semaphore = new(1);

        public VertexBufferPool()
        {
            device = D3D11DeviceManager.Device.As<ID3D11Device>();
        }

        public const int MaxFreeBuffers = 64;
        public const int ResizeSmallerAt = 32;

        public ComPtr<ID3D11Buffer> Rent(int minCapacity)
        {
            int size = minCapacity * sizeof(T);
            BufferDesc desc;

            semaphore.Wait();
            try
            {
                for (int i = 0; i < freeBuffers.Count; i++)
                {
                    ComPtr<ID3D11Buffer> buffer = freeBuffers[i];
                    buffer.GetDesc(&desc);
                    if (desc.ByteWidth >= size)
                    {
                        freeBuffers.RemoveAt(i);
                        return buffer;
                    }
                }

                if (freeBuffers.Count > ResizeSmallerAt)
                {
                    ComPtr<ID3D11Buffer> buffer = freeBuffers[0];
                    freeBuffers.RemoveAt(0);
                    Resize(ref buffer, minCapacity);
                    return buffer;
                }

                desc = new()
                {
                    BindFlags = (uint)BindFlag.VertexBuffer,
                    CPUAccessFlags = (uint)CpuAccessFlag.Write,
                    MiscFlags = 0,
                    ByteWidth = (uint)size,
                    Usage = Usage.Dynamic
                };

                device.CreateBuffer(&desc, null, out var newBuffer);
                buffers.Add(newBuffer);

                return newBuffer;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Return(ComPtr<ID3D11Buffer> buffer)
        {
            semaphore.Wait();
            try
            {
                freeBuffers.Add(buffer);
                if (freeBuffers.Count > MaxFreeBuffers)
                {
                    ComPtr<ID3D11Buffer> freeBuffer = freeBuffers[0];
                    freeBuffers.RemoveAt(0);
                    freeBuffer.Release();
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Resize(ref ComPtr<ID3D11Buffer> buffer, int capacity)
        {
            int size = capacity * sizeof(T);
            BufferDesc desc;
            buffer.GetDesc(&desc);
            if (desc.ByteWidth > size)
            {
                return;
            }

            semaphore.Wait();
            try
            {
                buffers.Remove(buffer);
                buffer.Dispose();
                desc = new()
                {
                    BindFlags = (uint)BindFlag.VertexBuffer,
                    CPUAccessFlags = (uint)CpuAccessFlag.Write,
                    MiscFlags = 0,
                    ByteWidth = (uint)size,
                    Usage = Usage.Dynamic
                };
                device.CreateBuffer(&desc, null, out buffer);
                buffers.Add(buffer);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}