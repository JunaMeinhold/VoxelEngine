namespace VoxelEngine.Graphics.Buffers
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Resources;

    public class IndexBuffer : Resource
    {
        private bool isDirty;
        private int indexCount;
        private int indexCapacity;
        private readonly List<int> indices;
        private ID3D11Buffer indexBuffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer()
        {
            indices = new List<int>();
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer(ID3D11Device device, IEnumerable<int> indices)
        {
            this.indices = new List<int>(indices);
            ResizeBuffers(device);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer(ID3D11Device device, int capacity)
        {
            indices = new List<int>(capacity);
            ResizeBuffers(device);
        }

        public int Count => indexCount;

        public int IndexCapacity => indexCapacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeBuffers(ID3D11Device device)
        {
            if (indices.Count <= IndexCapacity)
            {
                return;
            }

            indexBuffer?.Dispose();
            indexBuffer = device.CreateBuffer((ReadOnlySpan<int>)indices.ToArray().AsSpan(),
                new BufferDescription(
                indices.Count * sizeof(int),
                BindFlags.IndexBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write));
            indexBuffer.DebugName = nameof(IndexBuffer);
            indexCapacity = indices.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void UpdateBuffers(ID3D11DeviceContext context)
        {
            DeviceHelper.Write(context, indexBuffer, indices.ToArray());
            indexCount = indices.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(int vertex)
        {
            indices.Add(vertex);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(params int[] vertices)
        {
            indices.AddRange(vertices);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            indices.Clear();
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context)
        {
            if (isDirty)
            {
                ResizeBuffers(context.Device);
                UpdateBuffers(context);

                isDirty = false;
            }

            context.IASetIndexBuffer(indexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
        }

        protected override void Dispose(bool disposing)
        {
            indexBuffer?.Dispose();
            indexBuffer = null;
        }
    }
}