using HexaEngine.Windows;
using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace HexaEngine.Objects.VoxelGen
{
    public class BlockVertexBuffer
    {
        private ID3D11Buffer vertexBuffer;
        private VertexBufferView bufferView;

        public BlockVertexBuffer()
        {
        }

        public int Used;
        public bool Dirty = true;
        public bool Initialised = false;
        public int VertexCount;

        public int[] Data;

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

        public void BufferData()
        {
            if (Used > 0 && Dirty)
            {
                VertexCount = Used;
                vertexBuffer?.Dispose();
                vertexBuffer = null;
                vertexBuffer = DeviceManager.Current.ID3D11Device.CreateBuffer(Data.AsSpan(0, Used).ToArray(), new()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = Marshal.SizeOf<int>() * Used,
                    Usage = ResourceUsage.Default
                });
                vertexBuffer.DebugName = "VetexBuffer Chunk";
                bufferView = new(vertexBuffer, Marshal.SizeOf<int>());

                Dirty = false;

                // Clear the data from memory as it is now stored on the GPU
                Data = null;
            }
        }

        public bool Render(ID3D11DeviceContext context)
        {
            if (vertexBuffer is null) return false;
            context.IASetVertexBuffers(0, bufferView);
            context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            return true;
        }

        public void Unload()
        {
            vertexBuffer?.Dispose();
            vertexBuffer = null;
        }
    }
}