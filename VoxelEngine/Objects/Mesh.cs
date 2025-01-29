﻿namespace VoxelEngine.Objects
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public abstract class Mesh<TVertex, TIndex> : Resource where TVertex : unmanaged where TIndex : unmanaged
    {
        public Mesh()
        {
            Initialize();
        }

        public VertexBuffer<TVertex> VertexBuffer;
        public IndexBuffer<TIndex> IndexBuffer;

        public bool HasVertexBuffer => VertexBuffer != null;

        public bool HasIndexBuffer => IndexBuffer != null;

        protected abstract void Initialize();

        protected virtual void Uninitialize()
        {
            VertexBuffer?.Dispose();
            VertexBuffer = null;
            IndexBuffer?.Dispose();
            IndexBuffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BindType Bind(ComPtr<ID3D11DeviceContext> context)
        {
            if (HasIndexBuffer)
            {
                VertexBuffer.Bind(context);
                IndexBuffer.Bind(context);
                return BindType.Indexed;
            }
            if (HasVertexBuffer)
            {
                VertexBuffer.Bind(context);
                return BindType.Vertex;
            }
            return BindType.None;
        }

        public BindType Bind(ComPtr<ID3D11DeviceContext> context, int slot)
        {
            if (HasIndexBuffer)
            {
                VertexBuffer.Bind(context, slot);
                IndexBuffer.Bind(context);
                return BindType.Indexed;
            }
            if (HasVertexBuffer)
            {
                VertexBuffer.Bind(context, slot);
                return BindType.Vertex;
            }
            return BindType.None;
        }

        public void DrawAuto(ComPtr<ID3D11DeviceContext> context, GraphicsPipelineState pso)
        {
            if (HasIndexBuffer)
            {
                VertexBuffer.Bind(context, 0);
                IndexBuffer.Bind(context);
                pso.Begin(context);
                context.DrawIndexed((uint)IndexBuffer.Count, 0, 0);
                return;
            }
            if (HasVertexBuffer)
            {
                VertexBuffer.Bind(context, 0);
                pso.Begin(context);
                context.Draw((uint)VertexBuffer.Count, 0);
                return;
            }
        }

        protected override void DisposeCore()
        {
            Uninitialize();
        }
    }
}