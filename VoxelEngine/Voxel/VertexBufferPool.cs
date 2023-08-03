namespace VoxelEngine.Voxel
{
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;

    public class VertexBufferPool<T>
    {
        private readonly ID3D11Device device;
        private readonly List<ID3D11Buffer> buffers = new();
        private readonly List<ID3D11Buffer> freeBuffers = new();

        public VertexBufferPool(ID3D11Device device)
        {
            this.device = device;
        }

        public const int MaxFreeBuffers = 64;
        public const int ResizeSmallerAt = 32;

        public ID3D11Buffer Rent(int minCapacity)
        {
            int size = minCapacity * Marshal.SizeOf<T>();
            for (int i = 0; i < freeBuffers.Count; i++)
            {
                ID3D11Buffer buffer = freeBuffers[i];
                if (buffer.Description.ByteWidth >= size)
                {
                    freeBuffers.RemoveAt(i);
                    return buffer;
                }
            }

            if (freeBuffers.Count > ResizeSmallerAt)
            {
                ID3D11Buffer buffer = freeBuffers[0];
                freeBuffers.RemoveAt(0);
                Resize(ref buffer, minCapacity);
                return buffer;
            }

            ID3D11Buffer newBuffer = device.CreateBuffer(new()
            {
                BindFlags = BindFlags.VertexBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
                MiscFlags = ResourceOptionFlags.None,
                ByteWidth = size,
                Usage = ResourceUsage.Dynamic
            });
            buffers.Add(newBuffer);
            return newBuffer;
        }

        public void Return(ID3D11Buffer buffer)
        {
            freeBuffers.Add(buffer);
            if (freeBuffers.Count > MaxFreeBuffers)
            {
                ID3D11Buffer freeBuffer = freeBuffers[0];
                freeBuffers.RemoveAt(0);
                freeBuffer.Dispose();
            }
        }

        public void Resize(ref ID3D11Buffer buffer, int capacity)
        {
            int size = capacity * Marshal.SizeOf<T>();
            if (buffer.Description.ByteWidth > size)
            {
                return;
            }

            buffers.Remove(buffer);
            buffer.Dispose();
            buffer = device.CreateBuffer(new()
            {
                BindFlags = BindFlags.VertexBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
                MiscFlags = ResourceOptionFlags.None,
                ByteWidth = size,
                Usage = ResourceUsage.Dynamic
            });
            buffers.Add(buffer);
        }
    }
}