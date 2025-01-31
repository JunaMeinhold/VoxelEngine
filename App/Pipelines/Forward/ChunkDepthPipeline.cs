namespace App.Pipelines.Forward
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Voxel;

    public class ChunkDepthPipeline : DisposableBase
    {
        private readonly GraphicsPipelineState pipeline;
        private readonly ConstantBuffer<Matrix4x4> mvpBuffer;
        private readonly ConstantBuffer<WorldData> worldDataBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 ChunkOffset;
            public float Padd;
        }

        public ChunkDepthPipeline()
        {
            pipeline = GraphicsPipelineState.Create(new()
            {
                VertexShader = "forward/depth/voxel/vs.hlsl",
                PixelShader = "forward/depth/voxel/ps.hlsl",
            }, GraphicsPipelineStateDesc.Default);

            mvpBuffer = new(CpuAccessFlags.Write);
            worldDataBuffer = new(CpuAccessFlags.Write);
            pipeline.Bindings.SetCBV("MatrixBuffer", mvpBuffer);
            pipeline.Bindings.SetCBV("WorldData", worldDataBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(GraphicsContext context, Chunk chunk)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Update(context, new WorldData() { ChunkOffset = chunk.Position });
        }

        protected override void DisposeCore()
        {
            pipeline.Dispose();
            mvpBuffer.Dispose();
            worldDataBuffer.Dispose();
        }
    }
}