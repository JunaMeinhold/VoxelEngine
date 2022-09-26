namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Buffers;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Voxel;

    public class ChunkDepthPipeline : Pipeline
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
            Rasterizer = RasterizerDescription.CullBack,
            DepthStencil = DepthStencilDescription.Default,
            Blend = BlendDescription.Opaque,
            Topology = PrimitiveTopology.TriangleList,
        })
        {
            mvpBuffer = new(device, ShaderStage.Vertex, 0);
            worldDataBuffer = new(device, ShaderStage.Vertex, 1);
            ConstantBuffers.Add(mvpBuffer.Buffer, ShaderStage.Vertex, 0);
            ConstantBuffers.Add(worldDataBuffer.Buffer, ShaderStage.Vertex, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, Chunk chunk)
        {
            mvpBuffer.Write(context, new ModelViewProjBuffer(view, Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Write(context, new WorldData() { chunkOffset = chunk.Position });
        }
    }
}