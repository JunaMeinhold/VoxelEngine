namespace App.Renderers
{
    using App.Pipelines.Deferred;
    using App.Pipelines.Forward;
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System;
    using System.Numerics;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public unsafe class WorldRenderer : BaseRenderComponent
    {
        private ChunkGeometryPass geometryPass;
        private CSMChunkPipeline csmPass;
        private World? world;
        private bool debugChunksRegion;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Geometry;

        public bool DebugChunksRegion { get => debugChunksRegion; set => debugChunksRegion = value; }

        public override void Awake()
        {
            if (GameObject is not World world)
            {
                throw new InvalidOperationException("WorldRenderer only works on World");
            }

            geometryPass = new();
            csmPass = new();

            this.world = world;
        }

        public override void Destroy()
        {
            geometryPass.Dispose();
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

        private void DeferredPass(GraphicsContext context, Camera camera)
        {
            if (world == null) return;
            geometryPass.Begin(context);
            var frustum = camera.Transform.Frustum;
            for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
            {
                RenderRegion region = world.LoadedRenderRegions[j];
                if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && frustum.Intersects(region.BoundingBox))
                {
                    geometryPass.Update(context);
                    region.Bind(context);
                    context.DrawInstanced((uint)region.VertexBuffer.VertexCount, 1, 0, 0);
                }
                if (debugChunksRegion)
                {
                    DebugDraw.DrawBoundingBox(region.Name, region.BoundingBox, new(1, 1, 0, 0.8f));
                }
            }
            if (debugChunksRegion)
            {
                for (int j = 0; j < world.LoadedChunkSegments.Count; j++)
                {
                    ChunkSegment chunk = world.LoadedChunkSegments[j];
                    Vector3 min = new Vector3(chunk.Position.X, 0, chunk.Position.Y) * Chunk.CHUNK_SIZE;
                    Vector3 max = min + new Vector3(Chunk.CHUNK_SIZE) * new Vector3(1, WorldMap.CHUNK_AMOUNT_Y, 1);
                    DebugDraw.DrawBoundingBox($"{chunk.Position}+0", new(min, max), new(1, 1, 1, 0.4f));
                }
            }
            geometryPass.End(context);
        }

        private void DirectionalLightShadowPass(GraphicsContext context, Camera camera, DirectionalLight light)
        {
            if (world == null) return;

            csmPass.Begin(context);
            var frustra = light.ShadowFrustra;
            for (int i = 0; i < world.LoadedRenderRegions.Count; i++)
            {
                RenderRegion region = world.LoadedRenderRegions[i];
                for (int j = 0; j < light.CascadeCount; j++)
                {
                    var frustum = frustra[j];
                    if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && frustum.Intersects(region.BoundingBox))
                    {
                        csmPass.Update(context);
                        region.Bind(context);
                        context.DrawInstanced((uint)region.VertexBuffer.VertexCount, 1, 0, 0);
                        break;
                    }
                }
            }
            csmPass.End(context);
        }
    }
}