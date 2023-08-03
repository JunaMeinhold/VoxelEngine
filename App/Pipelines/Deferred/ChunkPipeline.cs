namespace App.Pipelines.Deferred
{
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Buffers;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;

    public class ChunkPrepassPipeline : Pipeline
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

        public ChunkPrepassPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "deferred/prepass/voxel/vs.hlsl",
            PixelShader = "deferred/prepass/voxel/ps.hlsl",
            Rasterizer = RasterizerDescription.CullBack,
            DepthStencil = DepthStencilDescription.Default,
            Blend = BlendDescription.Opaque,
            Topology = PrimitiveTopology.TriangleList,
        })
        {
            textures = new();
            textures.Sampler = device.CreateSamplerState(SamplerDescription.PointWrap);
            textures.Load(device, BlockRegistry.Textures.ToArray());

            mvpBuffer = new(device);
            worldDataBuffer = new(device);
            blockBuffer = new(device, 256);

            ShaderResourceViews.Append(textures, ShaderStage.Pixel);
            ConstantBuffers.AppendRange(new ID3D11Buffer[] { mvpBuffer, worldDataBuffer, blockBuffer }, ShaderStage.Vertex);
            SamplerStates.Append(textures.Sampler, ShaderStage.Pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, Chunk chunk)
        {
            mvpBuffer.Write(context, new ModelViewProjBuffer(view, Matrix4x4.CreateTranslation(chunk.Position * Chunk.CHUNK_SIZE)));
            worldDataBuffer.Write(context, new WorldData() { chunkOffset = chunk.Position });
            blockBuffer.Write(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, RenderRegion region)
        {
            mvpBuffer.Write(context, new ModelViewProjBuffer(view, Matrix4x4.Identity));
            worldDataBuffer.Write(context, new WorldData() { chunkOffset = Vector3.Zero });
            blockBuffer.Write(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        public override void Dispose()
        {
            base.Dispose();
            textures.Dispose();
        }
    }
}