namespace App.Pipelines.Deferred
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Voxel.Blocks;

    public class ChunkGeometryPass : RenderPass
    {
        private readonly Texture2D textures;
        private readonly SamplerState samplerState;
        private readonly ConstantBuffer<Matrix4x4> mvpBuffer;
        private readonly ConstantBuffer<WorldData> worldDataBuffer;
        private readonly ConstantBuffer<BlockDescriptionPacked> blockBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;
        }

        public ChunkGeometryPass()
        {
            textures = new([.. BlockRegistry.Textures]);
            samplerState = new SamplerState(SamplerDescription.PointWrap);

            mvpBuffer = new(CpuAccessFlags.Write);
            worldDataBuffer = new(CpuAccessFlags.Write);
            blockBuffer = new(CpuAccessFlags.Write, 256);

            state.Bindings.SetSRV("shaderTexture", textures);
            state.Bindings.SetSampler("Sampler", samplerState);
            state.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            state.Bindings.SetCBV("WorldData", worldDataBuffer);
            state.Bindings.SetCBV("TexData", blockBuffer);
        }

        protected override GraphicsPipelineState CreatePipelineState()
        {
            InputElementDescription[] inputElements =
            {
                new("POSITION", 0, Format.R32Sint, 0, -1, InputClassification.PerVertexData, 0),
                new("POSITION", 1, Format.R32G32B32Float, 0, -1, InputClassification.PerVertexData, 0),
                new("COLOR", 0, Format.R8G8B8A8Unorm, 0, -1, InputClassification.PerVertexData, 0),
            };
            return GraphicsPipelineState.Create(new()
            {
                VertexShader = "deferred/voxel/vs.hlsl",
                PixelShader = "deferred/voxel/ps.hlsl",
            }, new()
            {
                InputElements = inputElements
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(GraphicsContext context)
        {
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.Identity));
            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
            blockBuffer.Update(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            textures.Dispose();
            samplerState.Dispose();
            mvpBuffer.Dispose();
            worldDataBuffer.Dispose();
            blockBuffer.Dispose();
        }
    }
}