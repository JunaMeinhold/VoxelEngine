namespace App.Pipelines.Deferred
{
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;

    public class ChunkGeometryPipeline : GraphicsPipeline
    {
        private readonly Texture2DArray textures;
        private readonly ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private readonly ConstantBuffer<WorldData> worldDataBuffer;
        private readonly ConstantBuffer<BlockDescriptionPacked> blockBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;
        }

        public ChunkGeometryPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "deferred/prepass/voxel/vs.hlsl",
            PixelShader = "deferred/prepass/voxel/ps.hlsl",
        }, GraphicsPipelineState.Default)
        {
            textures = new();
            textures.Sampler = device.CreateSamplerState(SamplerDescription.PointWrap);
            textures.Load(device, BlockRegistry.Textures.ToArray());

            mvpBuffer = new(device, CpuAccessFlags.Write);
            worldDataBuffer = new(device, CpuAccessFlags.Write);
            blockBuffer = new(device, CpuAccessFlags.Write, 256);

            ShaderResourceViews.Append(textures, ShaderStage.Pixel);
            ConstantBuffers.AppendRange(new ID3D11Buffer[] { mvpBuffer, worldDataBuffer, blockBuffer }, ShaderStage.Vertex);
            SamplerStates.Append(textures.Sampler, ShaderStage.Pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view)
        {
            mvpBuffer.Update(context, new ModelViewProjBuffer(view, Matrix4x4.Identity));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
            blockBuffer.Update(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        public override void Dispose()
        {
            base.Dispose();
            textures.Dispose();
        }
    }
}