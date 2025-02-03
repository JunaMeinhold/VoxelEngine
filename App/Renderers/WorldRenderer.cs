namespace App.Renderers
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DebugDraw;
    using Hexa.NET.DXGI;
    using System;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;

    public unsafe class WorldRenderer : BaseRenderComponent
    {
        private Texture2D textures;
        private SamplerState samplerState;

        private GraphicsPipelineState geometry;
        private GraphicsPipelineState csmPass;
        private World? world;
        private bool debugChunksRegion;
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private ConstantBuffer<WorldData> worldDataBuffer;
        private ConstantBuffer<BlockDescriptionPacked> blockBuffer;

        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;

            public WorldData(Vector3 chunkOffset)
            {
                this.chunkOffset = chunkOffset;
                padd = 0;
            }

            public WorldData(Vector2 chunkOffset)
            {
                this.chunkOffset = new(chunkOffset.X, 0, chunkOffset.Y);
                padd = 0;
            }
        }

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Geometry;

        public bool DebugChunksRegion { get => debugChunksRegion; set => debugChunksRegion = value; }

        public override void Awake()
        {
            if (GameObject is not World world)
            {
                throw new InvalidOperationException("WorldRenderer only works on World");
            }
            this.world = world;

            textures = new([.. BlockRegistry.Textures]);
            samplerState = new SamplerState(SamplerStateDescription.PointWrap);
            blockBuffer = new(256, CpuAccessFlags.Write);

            geometry = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "deferred/voxel/vs.hlsl",
                PixelShader = "deferred/voxel/ps.hlsl",
            }, new()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Blend = BlendDescription.Opaque,
                InputElements = (
                [
                    new("POSITION", 0, Format.R32G32B32Float, 0, -1, InputClassification.PerVertexData, 0),
                    new("POSITION", 1, Format.R32Sint, 0, -1, InputClassification.PerVertexData, 0),
                    new("COLOR", 0, Format.R8G8B8A8Unorm, 0, -1, InputClassification.PerVertexData, 0),
                ])
            });

            csmPass = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
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

            mvpBuffer = new(CpuAccessFlags.Write);
            worldDataBuffer = new(CpuAccessFlags.Write);

            geometry.Bindings.SetSRV("shaderTexture", textures);
            geometry.Bindings.SetSampler("Sampler", samplerState);
            geometry.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            geometry.Bindings.SetCBV("WorldData", worldDataBuffer);
            geometry.Bindings.SetCBV("TexData", blockBuffer);

            csmPass.Bindings.SetCBV("MatrixBuffer", mvpBuffer);
            csmPass.Bindings.SetCBV("WorldData", worldDataBuffer);
        }

        public override void Destroy()
        {
            textures.Dispose();
            samplerState.Dispose();
            mvpBuffer.Dispose();
            worldDataBuffer.Dispose();
            blockBuffer.Dispose();
            geometry.Dispose();
            csmPass.Dispose();
        }

        public override void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.DirectionalLightShadowPass && parameter is DirectionalLight light)
            {
                DirectionalLightShadowPass(context, camera, light);
            }
            else if (pass == PassIdentifer.DeferredPass)
            {
                DeferredPass(context, camera);
            }
        }

        private bool dirty = true;

        private void Update(GraphicsContext context)
        {
            if (!dirty) return;
            dirty = false;

            worldDataBuffer.Update(context, new WorldData() { chunkOffset = Vector3.Zero });
            blockBuffer.Update(context, BlockRegistry.GetDescriptionPackeds().ToArray());
        }

        private void DeferredPass(GraphicsContext context, Camera camera)
        {
            if (world == null) return;
            Update(context);
            context.SetGraphicsPipelineState(geometry);
            var frustum = camera.Transform.Frustum;
            for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
            {
                RenderRegion region = world.LoadedRenderRegions[j];
                if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && frustum.Intersects(region.BoundingBox))
                {
                    if (region.Bind(context))
                    {
                        worldDataBuffer.Update(context, new WorldData(region.Offset));
                        context.DrawInstanced((uint)region.VertexBuffer.VertexCount, 1, 0, 0);
                    }
                }
                if (debugChunksRegion)
                {
                    DebugDraw.DrawBoundingBox(region.BoundingBox.Min, region.BoundingBox.Max, new(1, 1, 0, 0.8f));
                }
            }
            if (debugChunksRegion)
            {
                for (int j = 0; j < world.LoadedChunkSegments.Count; j++)
                {
                    ChunkSegment chunk = world.LoadedChunkSegments[j];
                    Vector3 min = new Vector3(chunk.Position.X, 0, chunk.Position.Y) * Chunk.CHUNK_SIZE;
                    Vector3 max = min + new Vector3(Chunk.CHUNK_SIZE) * new Vector3(1, WorldMap.CHUNK_AMOUNT_Y, 1);

                    DebugDraw.DrawBoundingBox(min, max, new(1, 1, 1, 0.4f));
                }
            }
            context.SetGraphicsPipelineState(null);
        }

        private void DirectionalLightShadowPass(GraphicsContext context, Camera camera, DirectionalLight light)
        {
            if (world == null) return;
            Update(context);
            context.SetGraphicsPipelineState(csmPass);
            var frustra = light.ShadowFrustra;
            for (int i = 0; i < world.LoadedRenderRegions.Count; i++)
            {
                RenderRegion region = world.LoadedRenderRegions[i];
                for (int j = 0; j < light.CascadeCount; j++)
                {
                    var frustum = frustra[j];
                    if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && frustum.Intersects(region.BoundingBox))
                    {
                        if (region.Bind(context))
                        {
                            worldDataBuffer.Update(context, new WorldData(region.Offset));
                            context.DrawInstanced((uint)region.VertexBuffer.VertexCount, 1, 0, 0);
                        }

                        break;
                    }
                }
            }
            context.SetGraphicsPipelineState(null);
        }
    }
}