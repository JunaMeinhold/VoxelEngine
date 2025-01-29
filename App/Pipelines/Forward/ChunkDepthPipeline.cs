namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Graphics.D3D11.Interfaces;
    using VoxelEngine.Voxel;

    public class ChunkDepthPipeline : GraphicsPipeline
    {
        private readonly ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private readonly ConstantBuffer<WorldData> worldDataBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;
        }

        public ChunkDepthPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "forward/depth/voxel/vs.hlsl",
            PixelShader = "forward/depth/voxel/ps.hlsl",
        }, GraphicsPipelineStateDesc.Default)
        {
            mvpBuffer = new(device, CpuAccessFlags.Write);
            worldDataBuffer = new(device, CpuAccessFlags.Write);
            ConstantBuffers.Add(mvpBuffer.Buffer, ShaderStage.Vertex, 0);
            ConstantBuffers.Add(worldDataBuffer.Buffer, ShaderStage.Vertex, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, Chunk chunk)
        {
            mvpBuffer.Update(context, new ModelViewProjBuffer(view, Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = chunk.Position });
        }
    }
}