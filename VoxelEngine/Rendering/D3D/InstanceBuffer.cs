namespace VoxelEngine.Rendering.D3D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Resources;

    public class InstanceBuffer : Resource
    {
        private bool isDirty;
        private List<Instance> instances;
        private int instanceCount;
        private ID3D11Buffer instanceBuffer;
        private string debugName;
        private readonly int size;

        public InstanceBuffer()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InstanceBuffer(ID3D11Device device, IEnumerable<Instance> instances)
        {
            size = Marshal.SizeOf<InstanceData>();
            this.instances = new(instances);
            ResizeBuffers(device);
            isDirty = true;
        }

        public InstanceBuffer(int capacity)
        {
            size = Marshal.SizeOf<InstanceData>();
            instances = new(capacity);
        }

        public int InstanceCount => instanceCount;

        public int InstanceCapacity { get; private set; }

        public string DebugName
        {
            get => debugName; set
            {
                debugName = value;
                if (instanceBuffer != null)
                {
                    instanceBuffer.DebugName = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeBuffers(ID3D11Device device)
        {
            if (instances.Count <= InstanceCapacity)
            {
                return;
            }

            instanceBuffer?.Dispose();
            instanceBuffer = device.CreateBuffer((ReadOnlySpan<InstanceData>)instances.Cast<InstanceData>().ToArray().AsSpan(),
                new BufferDescription(
                instances.Count * sizeof(InstanceData),
                BindFlags.VertexBuffer,
                ResourceUsage.Dynamic,
                CpuAccessFlags.Write));
            InstanceCapacity = instances.Count;
            instanceBuffer.DebugName = debugName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void UpdateBuffers(ID3D11DeviceContext context)
        {
            DeviceHelper.Write(context, instanceBuffer, instances.Cast<InstanceData>().ToArray());
            instanceCount = instances.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Instance instance)
        {
            instances.Add(instance);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(params Instance[] instances)
        {
            this.instances.AddRange(instances);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            instances.Clear();
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(Instance instance)
        {
            instances.Remove(instance);
            isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context, ID3D11Buffer vertexBuffer, int vertexSize)
        {
            Bind(context, vertexBuffer, vertexSize, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context, ID3D11Buffer vertexBuffer, int vertexSize, int slot)
        {
            if (isDirty)
            {
                ResizeBuffers(context.Device);
                UpdateBuffers(context);

                isDirty = false;
            }

            context.IASetVertexBuffers(slot, new ID3D11Buffer[] { vertexBuffer, instanceBuffer }, new int[] { vertexSize, size }, new int[] { 0, 0 });
        }

        protected override void Dispose(bool disposing)
        {
            instanceBuffer?.Dispose();
            instanceBuffer = null;
            instances.Clear();
            instances = null;
        }
    }
}