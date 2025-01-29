namespace App.Pipelines.Forward
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Voxel;

    public class CSMChunkPipeline : RenderPass
    {
        private readonly ConstantBuffer<Matrix4x4> mvpBuffer;
        private readonly ConstantBuffer<WorldData> worldDataBuffer;
        private readonly ConstantBuffer<Matrix4x4> cascadeBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;
        }

        public CSMChunkPipeline()
        {
            mvpBuffer = new(CpuAccessFlag.Write);
            worldDataBuffer = new(CpuAccessFlag.Write);
            cascadeBuffer = new(CpuAccessFlag.Write, 16);
            state.Bindings.SetCBV("MatrixBuffer", mvpBuffer);
            state.Bindings.SetCBV("WorldData", worldDataBuffer);
            state.Bindings.SetCBV("CascadeBuffer", cascadeBuffer);
        }

        protected override GraphicsPipelineState CreatePipelineState()
        {
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/csm/voxel/vs.hlsl",
                GeometryShader = "forward/csm/voxel/gs.hlsl",
            }, new GraphicsPipelineStateDesc()
            {
                Rasterizer = RasterizerDescription.CullFront,
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Update(ComPtr<ID3D11DeviceContext> context, Matrix4x4* views)
        {
            cascadeBuffer.Update(context, views, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ComPtr<ID3D11DeviceContext> context, Chunk chunk)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = chunk.Position });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ComPtr<ID3D11DeviceContext> context)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.Identity));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            mvpBuffer.Dispose();
            worldDataBuffer.Dispose();
            cascadeBuffer.Dispose();
        }
    }
}