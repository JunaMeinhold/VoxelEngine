namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Voxel;

    public class CSMChunkPipeline : GraphicsPipeline
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
        }, new GraphicsPipelineStateDesc()
        {
            Rasterizer = RasterizerDescription.CullFront,
        })
        {
            mvpBuffer = new(device, CpuAccessFlags.Write);
            worldDataBuffer = new(device, CpuAccessFlags.Write);
            cascadeBuffer = new(device, CpuAccessFlags.Write, 16);

            ConstantBuffers.AppendRange(new ID3D11Buffer[] { mvpBuffer, worldDataBuffer }, ShaderStage.Vertex);
            ConstantBuffers.Append(cascadeBuffer, ShaderStage.Geometry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Update(ID3D11DeviceContext context, Matrix4x4* views)
        {
            cascadeBuffer.Update(context, views, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, Chunk chunk)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = chunk.Position });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.Identity));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
        }
    }
}