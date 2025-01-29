namespace App.Renderers
{
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using App.Pipelines.Forward;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using VoxelEngine.Windows;

    public class MainSceneDeferredRenderer : ISceneRenderer
    {
        private ConstantBuffer<CBCamera> cameraBuffer;
        private ConstantBuffer<Matrix4x4> csmMatrixBuffer;

        private SwapChain swapChain;
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

        private DirectionalLight directionalLight;
        private int width;
        private int height;

        private bool debugChunksRegion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(GameWindow window)
        {
            swapChain = window.SwapChain;
            width = window.Width;
            height = window.Height;

            anisotropicClamp = new(SamplerDescription.AnisotropicClamp);
            pointClamp = new(SamplerDescription.PointClamp);
            linearClamp = new(SamplerDescription.LinearClamp);

            cameraBuffer = new(CpuAccessFlag.Write);

            D3D11GlobalResourceList.SetCBV("CameraBuffer", cameraBuffer);
            D3D11GlobalResourceList.SetSampler("linearClampSampler", linearClamp);

            depthStencil = new(window.Width, window.Height);
            gbuffer = new(window.Width, window.Height, Format.R16G16B16A16Float, Format.R8G8B8A8Unorm, Format.R16G16B16A16Float, Format.R16G16B16A16Float);
            lightBuffer = new(Format.R16G16B16A16Float, width, height, 1, 1, 0, GpuAccessFlags.RW);
            fxaaBuffer = new(Format.R16G16B16A16Float, window.Width, window.Height, 1, 1, 0, GpuAccessFlags.RW);
            csmBuffer = new(Format.D32Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, 5);
            csmMatrixBuffer = new(CpuAccessFlag.Write, 16);
            D3D11GlobalResourceList.SetCBV("CSMCascadeBuffer", csmMatrixBuffer);
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
            compose.Camera = cameraBuffer;

            fxaa = new();
            fxaa.Input = fxaaBuffer;

            hbao = new();
            hbao.Depth = depthStencil;
            hbao.Normal = gbuffer.SRVs[1];

            godRays = new(width, height);

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
        public unsafe void Render(ComPtr<ID3D11DeviceContext> context, Camera camera, Scene scene)
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

            DebugDraw.SetCamera(camera.Transform.ViewProjection);
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

            cameraBuffer.Update(context, new CBCamera(camera, new Vector2(width, height)));

            // Depth light pass.
            csmBuffer.Clear(context, ClearFlag.Depth, 1, 0);
            gbuffer.Clear(context, default);
            depthStencil.Clear(context, ClearFlag.Depth | ClearFlag.Stencil, 1, 0);

            context.ClearState();

            directionalLight.Update(context, camera, csmMatrixBuffer);

            SkyboxRenderer.SunDir = directionalLight.Transform.Forward;

            context.RSSetViewport(csmBuffer.Viewport);
            context.SetRenderTarget(null, csmBuffer);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);
            scene.RenderSystem.Draw(context, RenderQueueIndex.GeometryLast, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);

            context.ClearState();

            gbuffer.SetTarget(context, depthStencil);
            context.RSSetViewport(gbuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DeferredPass, camera);

            hbao.Update(context, camera, hbaoBuffer.Viewport);
            context.SetRenderTarget(hbaoBuffer);
            context.RSSetViewport(hbaoBuffer.Viewport);
            hbao.Pass(context);

            godRays.Update(context, camera, directionalLight);
            godRays.PrePass(context, depthStencil);

            context.ClearState();
            Vector4 col = default;
            context.ClearRenderTargetView(lightBuffer, (float*)&col);
            context.SetRenderTarget(lightBuffer, depthStencil);
            context.RSSetViewport(lightBuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Background, PassIdentifer.ForwardPass, camera);

            // light pass
            lightPipeline.Update(context, new(camera, new Vector2(width, height)), directionalLight.DirectionalLightShadowData);
            lightPipeline.Pass(context);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Transparent, PassIdentifer.ForwardPass, camera);

            bloom.Update(context);
            bloom.Pass(context, lightBuffer);

            context.SetRenderTarget(lightBuffer);
            context.RSSetViewport(lightBuffer.Viewport);
            godRays.Pass(context);

            context.ClearRenderTargetView(fxaaBuffer, (float*)&col);
            context.SetRenderTarget(fxaaBuffer);
            context.RSSetViewport(fxaaBuffer.Viewport);
            compose.Pass(context);

            swapChain.ClearTarget(context, default);
            swapChain.SetTarget(context, false);
            fxaa.Pass(context);

            swapChain.SetTarget(context, depthStencil);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Overlay, PassIdentifer.ForwardPass, camera);

            context.ClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            cameraBuffer.Dispose();
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