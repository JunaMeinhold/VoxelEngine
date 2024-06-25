namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Voxel;

    public class CSMChunkPipeline : Pipeline
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

        public CSMChunkPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "forward/csm/voxel/vs.hlsl",
            GeometryShader = "forward/csm/voxel/gs.hlsl",
            Rasterizer = RasterizerDescription.CullFront,
            DepthStencil = DepthStencilDescription.Default,
            Blend = BlendDescription.Opaque,
            Topology = Vortice.Direct3D.PrimitiveTopology.TriangleList,
        })
        {
            mvpBuffer = new(device);
            worldDataBuffer = new(device);
            cascadeBuffer = new(device, 16);

            ConstantBuffers.AppendRange(new ID3D11Buffer[] { mvpBuffer, worldDataBuffer }, ShaderStage.Vertex);
            ConstantBuffers.Append(cascadeBuffer, ShaderStage.Geometry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, Matrix4x4[] views)
        {
            cascadeBuffer.Write(context, views);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, Chunk chunk)
        {
            mvpBuffer.Write(context, Matrix4x4.Transpose(Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Write(context, new WorldData() { chunkOffset = chunk.Position });
        }
    }
}