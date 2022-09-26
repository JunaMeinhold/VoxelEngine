namespace VoxelEngine.Voxel
{
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;

    public class BlockVertexBuffer : IDisposable
    {
        private ID3D11Buffer vertexBuffer;
        private int size;

        public int Used;
        public bool Dirty = true;
        public bool Initialised;
        public int VertexCount;

        public int[] Data;
        private string debugName;

        public string DebugName
        {
            get => debugName;
            set
            {
                debugName = value;
                if (vertexBuffer != null)
                {
                    vertexBuffer.DebugName = value;
                }
            }
        }

        public BlockVertexBuffer()
        {
            size = Marshal.SizeOf<int>();
        }

        public void Reset(int length)
        {
            Used = 0;
            Data = new int[length];
            Dirty = true;
        }

        public void Extend(int amount)
        {
            int[] newData = new int[Data.Length + amount];
            Array.Copy(Data, newData, Data.Length);
            Data = newData;
        }

        public void BufferData(ID3D11Device device)
        {
            if (Used > 0 && Dirty)
            {
                VertexCount = Used;
                vertexBuffer?.Dispose();
                vertexBuffer = null;
                try
                {
                    vertexBuffer = device.CreateBuffer((ReadOnlySpan<int>)Data.AsSpan(0, Used), new()
                    {
                        BindFlags = BindFlags.VertexBuffer,
                        CPUAccessFlags = CpuAccessFlags.None,
                        MiscFlags = ResourceOptionFlags.None,
                        ByteWidth = Marshal.SizeOf<int>() * Used,
                        Usage = ResourceUsage.Immutable
                    });
                }
                catch
                {
                    throw new Exception(device.DeviceRemovedReason.ToString());
                }

                vertexBuffer.DebugName = debugName;

                Dirty = false;

                // Clear the data from memory as it is now stored on the GPU
                Data = null;
            }
        }

        public bool Bind(ID3D11DeviceContext context)
        {
            if (vertexBuffer is null)
            {
                return false;
            }

            context.IASetVertexBuffer(0, vertexBuffer, size);
            context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            return true;
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            vertexBuffer = null;
            Data = null;
            GC.SuppressFinalize(this);
        }
    }
}