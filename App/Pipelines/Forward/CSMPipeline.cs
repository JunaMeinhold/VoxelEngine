namespace App.Pipelines.Forward
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Voxel;

    public class CSMChunkPipeline : RenderPass
    {
        private readonly ConstantBuffer<Matrix4x4> mvpBuffer;
        private readonly ConstantBuffer<WorldData> worldDataBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;
        }

        public CSMChunkPipeline()
        {
            mvpBuffer = new(CpuAccessFlags.Write);
            worldDataBuffer = new(CpuAccessFlags.Write);
            state.Bindings.SetCBV("MatrixBuffer", mvpBuffer);
            state.Bindings.SetCBV("WorldData", worldDataBuffer);
        }

        protected override GraphicsPipelineState CreatePipelineState()
        {
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/csm/voxel/vs.hlsl",
                GeometryShader = "forward/csm/voxel/gs.hlsl",
                PixelShader = "forward/csm/voxel/ps.hlsl",
            }, new()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullNone,
                Blend = BlendDescription.Opaque,
                Topology = PrimitiveTopology.Trianglelist,
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(GraphicsContext context, Chunk chunk)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = chunk.Position });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(GraphicsContext context)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.Identity));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            mvpBuffer.Dispose();
            worldDataBuffer.Dispose();
        }
    }
}