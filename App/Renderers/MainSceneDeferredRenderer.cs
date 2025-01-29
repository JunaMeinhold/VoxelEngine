namespace App.Renderers
{
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using App.Pipelines.Forward;
    using App.Renderers.Forward;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using VoxelEngine.Windows;

    public class MainSceneDeferredRenderer : ISceneRenderer
    {
        private ConstantBuffer<CBCamera> camera;

        private SwapChain swapChain;
        private ChunkGeometryPass chunkPrepass;
        private CSMChunkPipeline chunkDepthPrepassCSM;
        private DeferredLightPass lightPipeline;
        private ComposeEffect compose;
        private FXAAEffect fxaa;
        private HBAOEffect hbao;
        private GodRaysEffect godRays;
        private BloomEffect bloom;

        private SamplerState anisotropicClamp;
        private SamplerState pointClamp;
        private SamplerState linearClamp;

        private DepthStencil depthStencil;
        private GBuffer gbuffer;
        private Texture2D lightBuffer;
        private Texture2D fxaaBuffer;

        private Texture2D hbaoBuffer;
        private DepthStencil csmBuffer;

        private PerlinNoiseWidget perlinNoiseWidget;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[16];

        private DirectionalLight directionalLight;
        private CBDirectionalLightSD directionalLightCB = new();
        private int width;
        private int height;

        private bool debugChunksRegion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(GameWindow window)
        {
            for (int i = 0; i < 16; i++)
            {
                ShadowFrustra[i] = new();
            }
            swapChain = window.SwapChain;
            width = window.Width;
            height = window.Height;

            anisotropicClamp = new(SamplerDescription.AnisotropicClamp);
            pointClamp = new(SamplerDescription.PointClamp);
            linearClamp = new(SamplerDescription.LinearClamp);

            camera = new(CpuAccessFlag.Write);

            D3D11GlobalResourceList.SetCBV("CameraBuffer", camera);
            D3D11GlobalResourceList.SetSampler("linearClampSampler", linearClamp);

            depthStencil = new(window.Width, window.Height);
            gbuffer = new(window.Width, window.Height, Format.R16G16B16A16Float, Format.R8G8B8A8Unorm, Format.R16G16B16A16Float, Format.R16G16B16A16Float);
            lightBuffer = new(Format.R16G16B16A16Float, width, height, 1, 1, 0, GpuAccessFlags.RW);
            fxaaBuffer = new(Format.R16G16B16A16Float, window.Width, window.Height, 1, 1, 0, GpuAccessFlags.RW);
            csmBuffer = new(Format.D32Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, 5);
            hbaoBuffer = new(Format.R32Float, width, height, 1, 1, 0, GpuAccessFlags.RW);

            lightPipeline = new();

            lightPipeline.Bindings.SetSRV("GBufferA", gbuffer.SRVs[0]);
            lightPipeline.Bindings.SetSRV("GBufferB", gbuffer.SRVs[1]);
            lightPipeline.Bindings.SetSRV("GBufferC", gbuffer.SRVs[2]);
            lightPipeline.Bindings.SetSRV("GBufferD", gbuffer.SRVs[3]);

            lightPipeline.Bindings.SetSRV("depthTexture", depthStencil);
            lightPipeline.Bindings.SetSRV("lightDepthMap", csmBuffer);
            lightPipeline.Bindings.SetSRV("aoTexture", hbaoBuffer);

            bloom = new(width, height);

            compose = new();
            compose.Input = lightBuffer;
            compose.Bloom = bloom.Output;
            compose.Depth = depthStencil;
            compose.Camera = camera;

            fxaa = new();
            fxaa.Input = fxaaBuffer;

            hbao = new();
            hbao.Depth = depthStencil;
            hbao.Normal = gbuffer.SRVs[1];

            godRays = new(width, height);

            chunkPrepass = new();
            chunkDepthPrepassCSM = new();

            directionalLight = new();
            directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 1.4f;
            directionalLight.Transform.Far = 100;
            directionalLight.Transform.Rotation = new(0, 100, 0);
            directionalLight.CastShadows = true;
            Keyboard.KeyUp += Keyboard_OnKeyUp;

            perlinNoiseWidget = new();
        }

        private void Keyboard_OnKeyUp(object sender, VoxelEngine.Core.Input.Events.KeyboardEventArgs e)
        {
            if (e.KeyCode == Key.F1)
            {
                if (Time.TimeScale > 0)
                {
                    Time.TimeScale = 0;
                }
                else
                {
                    Time.TimeScale = 60 * 10;
                }
            }
            if (e.KeyCode == Key.F5)
            {
                debugChunksRegion = !debugChunksRegion;
            }
        }

        public void Resize(GameWindow window)
        {
            width = window.Width;
            height = window.Height;

            depthStencil.Resize(width, height);
            gbuffer.Resize(width, height);
            lightBuffer.Resize(width, height);
            fxaaBuffer.Resize(width, height);
            hbaoBuffer.Resize(width, height);

            hbao.Depth = depthStencil;
            hbao.Normal = gbuffer.SRVs[1];

            godRays.Resize(width, height);

            lightPipeline.Bindings.SetSRV("GBufferA", gbuffer.SRVs[0]);
            lightPipeline.Bindings.SetSRV("GBufferB", gbuffer.SRVs[1]);
            lightPipeline.Bindings.SetSRV("GBufferC", gbuffer.SRVs[2]);
            lightPipeline.Bindings.SetSRV("GBufferD", gbuffer.SRVs[3]);

            lightPipeline.Bindings.SetSRV("depthTexture", depthStencil);
            lightPipeline.Bindings.SetSRV("lightDepthMap", csmBuffer);
            lightPipeline.Bindings.SetSRV("aoTexture", hbaoBuffer);

            bloom.Resize(width, height);

            compose.Input = lightBuffer;
            compose.Bloom = bloom.Output;
            compose.Depth = depthStencil;

            fxaa.Input = fxaaBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Render(ComPtr<ID3D11DeviceContext> context, Camera view, SceneElementCollection elements)
        {
            perlinNoiseWidget.Draw(context);

            ImGui.Text($"{1 / Time.Delta} FPS / {Time.Delta * 1000}ms");
            ImGui.InputFloat("TimeScale", ref Time.TimeScale);
            float gt = Time.GameTime;
            if (ImGui.InputFloat("GameTime", ref gt))
            {
                Time.GameTime = gt;
            }

            float fogStart = compose.FogStart;
            if (ImGui.InputFloat("FogStart", ref fogStart))
            {
                compose.FogStart = fogStart;
            }

            float fogEnd = compose.FogEnd;
            if (ImGui.InputFloat("FogEnd", ref fogEnd))
            {
                compose.FogEnd = fogEnd;
            }

            DebugDraw.SetCamera(view.Transform.ViewProjection);
            Vector3 rot = directionalLight.Transform.Rotation;
            rot.Y = 360 * Time.GameTimeNormalized - 90;
            float ro = rot.Y - 180F;
            directionalLight.Transform.Rotation = rot.NormalizeEulerAngleDegrees();
            if (rot.Y > 45 && rot.Y < 135)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1);
            }
            else if (rot.Y > 135 && rot.Y < 225)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(1, 0.2f, (rot.Y - 135) / 90);
            }
            else if (ro > 45 && ro < 135)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 0.2f;
            }
            else if (ro > 135 && ro < 225)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(0.2f, 1, (ro - 135) / 90);
            }
            directionalLight.Color *= 5;
            directionalLight.Transform.Recalculate();

            BoundingFrustum cameraFrustum = view.Transform.Frustum;

            camera.Update(context, new CBCamera(view, new Vector2(width, height)));

            // Depth light pass.
            csmBuffer.Clear(context, ClearFlag.Depth, 1, 0);
            gbuffer.Clear(context, default);
            depthStencil.Clear(context, ClearFlag.Depth | ClearFlag.Stencil, 1, 0);

            context.ClearState();

            for (int i = 0; i < elements.Count; i++)
            {
                GameObject element = elements[i];
                if (element is World world)
                {
                    CBDirectionalLightSD d = directionalLightCB;
                    Matrix4x4* views = CBDirectionalLightSD.GetViews(&d);
                    float* cascades = CBDirectionalLightSD.GetCascades(&d);
                    CSMHelper.GetLightSpaceMatrices(view, directionalLight.Transform, views, cascades, ShadowFrustra, 5);
                    directionalLightCB = d;
                    directionalLightCB.Color = directionalLight.Color;
                    SkyboxRenderer.SunDir = directionalLightCB.Direction = directionalLight.Transform.Forward;
                    directionalLightCB.CastShadows = directionalLight.CastShadows ? 1 : 0;
                    chunkDepthPrepassCSM.Update(context, views);
                    chunkDepthPrepassCSM.Begin(context);
                    context.RSSetViewport(csmBuffer.Viewport);
                    context.SetRenderTarget(null, csmBuffer);

                    for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
                    {
                        RenderRegion region = world.LoadedRenderRegions[j];
                        if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(region.BoundingBox))
                        {
                            chunkDepthPrepassCSM.Update(context);
                            region.Bind(context);
                            context.Draw((uint)region.VertexBuffer.VertexCount, 0);
                        }
                    }
                }
            }

            context.ClearState();

            // Deferred fill GBuffers pass.
            for (int i = 0; i < elements.Count; i++)
            {
                GameObject element = elements[i];
                if (element is World world)
                {
                    chunkPrepass.Begin(context);
                    gbuffer.SetTarget(context, depthStencil);

                    for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
                    {
                        RenderRegion region = world.LoadedRenderRegions[j];
                        if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(region.BoundingBox))
                        {
                            chunkPrepass.Update(context, view);
                            region.Bind(context);
                            context.Draw((uint)region.VertexBuffer.VertexCount, 0);
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
                }
            }

            hbao.Update(context, view, hbaoBuffer.Viewport);
            context.SetRenderTarget(hbaoBuffer);
            context.RSSetViewport(hbaoBuffer.Viewport);
            hbao.Pass(context);

            godRays.Update(context, view, directionalLight);
            godRays.PrePass(context, depthStencil);

            context.ClearState();
            context.ClearRenderTargetView(lightBuffer, default);

            // Forward pass.
            for (int i = 0; i < elements.ForwardComponents.Count; i++)
            {
                context.SetRenderTarget(lightBuffer, depthStencil);
                context.RSSetViewport(lightBuffer.Viewport);
                IForwardRenderComponent element = elements.ForwardComponents[i];

                element.DrawForward(context, view);
            }

            // light pass
            lightPipeline.Update(context, new(view, new Vector2(width, height)), directionalLightCB);
            context.SetRenderTarget(lightBuffer);
            context.RSSetViewport(lightBuffer.Viewport);
            lightPipeline.Pass(context);

            bloom.Update(context);
            bloom.Pass(context, lightBuffer);

            context.SetRenderTarget(lightBuffer);
            context.RSSetViewport(lightBuffer.Viewport);
            godRays.Pass(context);

            context.ClearRenderTargetView(fxaaBuffer, default);
            context.SetRenderTarget(fxaaBuffer);
            context.RSSetViewport(fxaaBuffer.Viewport);
            compose.Pass(context);

            swapChain.ClearTarget(context, default);
            swapChain.SetTarget(context, false);
            fxaa.Pass(context);

            context.ClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            camera.Dispose();
            chunkPrepass.Dispose();
            chunkDepthPrepassCSM.Dispose();
            lightPipeline.Dispose();
            fxaa.Dispose();
            hbao.Dispose();
            compose.Dispose();
            anisotropicClamp.Dispose();
            linearClamp.Dispose();
            depthStencil.Dispose();
            gbuffer.Dispose();
            fxaaBuffer.Dispose();
            hbaoBuffer.Dispose();
            lightBuffer.Dispose();
            csmBuffer.Dispose();

            perlinNoiseWidget.Release();
        }
    }
}