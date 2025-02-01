namespace App.Renderers
{
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using Hexa.NET.D3D11;
    using Hexa.NET.DebugDraw;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.ImPlot;
    using Hexa.NET.Mathematics;
    using HexaEngine.Graphics.Effects.Blur;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Unsafes;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;
    using VoxelEngine.Windows;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    [Flags]
    public enum PostFxFlags
    {
        None = 0,
        NoInput = 1 << 0,
        NoOutput = 1 << 1,
        PreDraw = 1 << 2,
    }

    public interface IPostFx
    {
        public string Name { get; }

        public bool Enabled { get; set; }

        public PostFxFlags Flags { get; }

        public void SetInput(IShaderResourceView srv, Viewport viewport);

        public void SetOutput(IRenderTargetView rtv, Viewport viewport);

        public void Update(ComPtr<ID3D11DeviceContext> context);

        public void PreDraw(ComPtr<ID3D11DeviceContext> context);

        public void Draw(ComPtr<ID3D11DeviceContext> context);
    }

    public abstract class PostFxBase : IPostFx
    {
        public abstract string Name { get; }

        public bool Enabled { get; set; }

        public abstract PostFxFlags Flags { get; }

        public virtual void Update(ComPtr<ID3D11DeviceContext> context)
        {
        }

        public virtual void Draw(ComPtr<ID3D11DeviceContext> context)
        {
        }

        public virtual void PreDraw(ComPtr<ID3D11DeviceContext> context)
        {
        }

        public virtual void SetInput(IShaderResourceView srv, Viewport viewport)
        {
        }

        public virtual void SetOutput(IRenderTargetView rtv, Viewport viewport)
        {
        }
    }

    public interface IPass
    {
        public void Execute(ComPtr<ID3D11DeviceContext> context);
    }

    public class PostProcessingPass : IPass
    {
        private readonly List<IPostFx> effects = [];

        public void Execute(ComPtr<ID3D11DeviceContext> context)
        {
        }
    }

    public class MainSceneDeferredRenderer : ISceneRenderer
    {
        private ConstantBuffer<CBCamera> cameraBuffer;
        private ConstantBuffer<CSMBuffer> csmBuffer;

        private SwapChain swapChain;
        private DeferredLightPass lightPipeline;
        private ComposeEffect compose;
        private FXAAEffect fxaa;
        private HBAOEffect hbao;
        private GodRaysEffect godRays;
        private BloomEffect bloom;

        private GaussianBlur blurFilter;

        private SamplerState anisotropicClampSampler;
        private SamplerState pointClampSampler;
        private SamplerState linearClampSampler;

        private DepthStencil depthStencil;

        private GBuffer gbuffer;
        private Texture2D lightBuffer;
        private Texture2D fxaaBuffer;

        private Texture2D hbaoBuffer;

        private PerlinNoiseWidget perlinNoiseWidget;
        private WorldProfilerWidget profilerWidget = new();

        private DirectionalLight directionalLight;
        private int rendererWidth;
        private int rendererHeight;

        private bool debugChunksRegion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(GameWindow window)
        {
            swapChain = window.SwapChain;
            rendererWidth = 1920;
            rendererHeight = 1080;

            anisotropicClampSampler = new(SamplerDescription.AnisotropicClamp);
            pointClampSampler = new(SamplerDescription.PointClamp);
            linearClampSampler = new(SamplerDescription.LinearClamp);

            cameraBuffer = new(CpuAccessFlags.Write);

            blurFilter = new(Format.R32G32Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize);

            directionalLight = new();
            directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 1.4f;
            directionalLight.Transform.Far = 100;
            directionalLight.Transform.Rotation = new(0, 100, 0);
            directionalLight.CastShadows = true;
            directionalLight.Create();

            D3D11GlobalResourceList.SetCBV("CameraBuffer", cameraBuffer);
            D3D11GlobalResourceList.SetSampler("linearClampSampler", linearClampSampler);
            D3D11GlobalResourceList.SetSampler("pointClampSampler", pointClampSampler);
            D3D11GlobalResourceList.SetSampler("anisotropicClampSampler", anisotropicClampSampler);

            depthStencil = new(rendererWidth, rendererHeight);
            gbuffer = new(rendererWidth, rendererHeight, Format.R16G16B16A16Float, Format.R8G8B8A8Unorm, Format.R16G16B16A16Float, Format.R16G16B16A16Float);
            D3D11GlobalResourceList.SetSRV("GBufferA", gbuffer.SRVs[0]);
            D3D11GlobalResourceList.SetSRV("GBufferB", gbuffer.SRVs[1]);
            D3D11GlobalResourceList.SetSRV("GBufferC", gbuffer.SRVs[2]);
            D3D11GlobalResourceList.SetSRV("GBufferD", gbuffer.SRVs[3]);
            D3D11GlobalResourceList.SetSRV("DepthTex", depthStencil);

            lightBuffer = new(Format.R16G16B16A16Float, rendererWidth, rendererHeight, 1, 1, 0, GpuAccessFlags.RW);
            fxaaBuffer = new(Format.R16G16B16A16Float, rendererWidth, rendererHeight, 1, 1, 0, GpuAccessFlags.RW);

            csmBuffer = new(CpuAccessFlags.Write);
            D3D11GlobalResourceList.SetCBV("CSMCascadeBuffer", csmBuffer);
            hbaoBuffer = new(Format.R32Float, rendererWidth, rendererHeight, 1, 1, 0, GpuAccessFlags.RW);

            lightPipeline = new();

            lightPipeline.Bindings.SetSRV("lightDepthMap", directionalLight.ShadowMap);
            lightPipeline.Bindings.SetSRV("aoTexture", hbaoBuffer);

            bloom = new(rendererWidth, rendererHeight);

            compose = new();
            compose.Input = lightBuffer;
            compose.Bloom = bloom.Output;
            compose.Camera = cameraBuffer;

            fxaa = new();
            fxaa.Input = fxaaBuffer;

            hbao = new();

            godRays = new(rendererWidth, rendererHeight);

            Keyboard.KeyUp += Keyboard_OnKeyUp;

            perlinNoiseWidget = new();
        }

        private void Keyboard_OnKeyUp(object? sender, VoxelEngine.Core.Input.Events.KeyboardEventArgs e)
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
        }

        public void ResizeRenderer()
        {
            depthStencil.Resize(rendererWidth, rendererHeight);
            gbuffer.Resize(rendererWidth, rendererHeight);
            lightBuffer.Resize(rendererWidth, rendererHeight);
            fxaaBuffer.Resize(rendererWidth, rendererHeight);
            hbaoBuffer.Resize(rendererWidth, rendererHeight);

            hbao.Depth = depthStencil;

            godRays.Resize(rendererWidth, rendererHeight);

            D3D11GlobalResourceList.SetSRV("GBufferA", gbuffer.SRVs[0]);
            D3D11GlobalResourceList.SetSRV("GBufferB", gbuffer.SRVs[1]);
            D3D11GlobalResourceList.SetSRV("GBufferC", gbuffer.SRVs[2]);
            D3D11GlobalResourceList.SetSRV("GBufferD", gbuffer.SRVs[3]);
            D3D11GlobalResourceList.SetSRV("DepthTex", depthStencil);

            lightPipeline.Bindings.SetSRV("aoTexture", hbaoBuffer);

            bloom.Resize(rendererWidth, rendererHeight);

            compose.Input = lightBuffer;
            compose.Bloom = bloom.Output;
            compose.Depth = depthStencil;

            fxaa.Input = fxaaBuffer;
        }

        private void FilterArray(GraphicsContext context, Texture2D source)
        {
            if (source.Width != blurFilter.Width || source.Height != blurFilter.Height || source.Format != blurFilter.Format)
            {
                blurFilter.Resize(source.Format, source.Width, source.Height);
            }

            for (int i = 0; i < source.ArraySize; i++)
            {
                blurFilter.Blur(context, source.SRVArraySlices![i], source.RTVArraySlices![i], source.Width, source.Height);
            }
        }

        private UnsafeRingBuffer<float> frames = new(512);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Render(GraphicsContext context, Camera camera, Scene scene)
        {
            const int shade_mode = 2;
            const float fill_ref = 0;
            double fill = shade_mode == 0 ? -double.PositiveInfinity : shade_mode == 1 ? double.PositiveInfinity : fill_ref;

            frames.Add(Time.Delta * 1000);
            perlinNoiseWidget.Draw(context);
            profilerWidget.Draw();
            ImPlot.SetNextAxesToFit();
            if (ImPlot.BeginPlot("Frames"))
            {
                ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
                ImPlot.PlotShaded("Frames", ref frames.Values[0], frames.Length, fill, 1, 0, ImPlotShadedFlags.None, frames.Head);
                ImPlot.PopStyleVar();

                ImPlot.PlotLine("Frames", ref frames.Values[0], frames.Length, 1, 0, ImPlotLineFlags.None, frames.Head);
                ImPlot.EndPlot();
            }

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

            ImGui.InputFloat("Light Bleeding", ref directionalLight.DirectionalLightShadowData.LightBleedingReduction);

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

            cameraBuffer.Update(context, new CBCamera(camera, new Vector2(rendererWidth, rendererHeight)));

            // Depth light pass.

            gbuffer.Clear(context, default);
            depthStencil.Clear(context, ClearFlag.Depth | ClearFlag.Stencil, 1, 0);

            context.ClearState();

            directionalLight.Update(context, camera, csmBuffer);

            SkyboxRenderer.SunDir = directionalLight.Transform.Forward;

            directionalLight.PrepareDraw(context);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);
            scene.RenderSystem.Draw(context, RenderQueueIndex.GeometryLast, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);

            FilterArray(context, directionalLight.ShadowMap);

            context.ClearState();

            gbuffer.SetTarget(context, depthStencil);
            context.SetViewport(gbuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DeferredPass, camera);

            context.ClearState();

            hbao.Update(context, camera, hbaoBuffer.Viewport);
            context.SetRenderTarget(hbaoBuffer);
            context.SetViewport(hbaoBuffer.Viewport);
            hbao.Pass(context);

            godRays.Update(context, camera, directionalLight);
            godRays.PrePass(context, depthStencil);

            context.ClearState();
            context.ClearRenderTargetView(lightBuffer, default);
            context.SetRenderTarget(lightBuffer, depthStencil);
            context.SetViewport(lightBuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Background, PassIdentifer.ForwardPass, camera);

            context.SetRenderTarget(lightBuffer);

            // light pass
            lightPipeline.Update(context, directionalLight.DirectionalLightShadowData);
            lightPipeline.Pass(context);

            context.SetRenderTarget(lightBuffer, depthStencil);

            scene.RenderSystem.Draw(context, RenderQueueIndex.GeometryLast, PassIdentifer.ForwardPass, camera);
            scene.RenderSystem.Draw(context, RenderQueueIndex.Transparent, PassIdentifer.ForwardPass, camera);

            context.ClearState();

            bloom.Update(context);
            bloom.Pass(context, lightBuffer);

            context.SetRenderTarget(lightBuffer);
            context.SetViewport(lightBuffer.Viewport);
            godRays.Pass(context);

            context.ClearRenderTargetView(fxaaBuffer, default);
            context.SetRenderTarget(fxaaBuffer);
            context.SetViewport(fxaaBuffer.Viewport);
            compose.Pass(context);

            swapChain.ClearTarget(context, default);
            swapChain.SetTarget(context, false);
            context.SetViewport(swapChain.Viewport);
            fxaa.Pass(context);

            swapChain.SetTarget(context, depthStencil);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Overlay, PassIdentifer.ForwardPass, camera);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            cameraBuffer.Dispose();
            csmBuffer.Dispose();

            lightPipeline.Dispose();
            compose.Dispose();
            fxaa.Dispose();
            hbao.Dispose();
            godRays.Dispose();
            bloom.Dispose();

            anisotropicClampSampler.Dispose();
            pointClampSampler.Dispose();
            linearClampSampler.Dispose();

            depthStencil.Dispose();
            gbuffer.Dispose();
            lightBuffer.Dispose();
            fxaaBuffer.Dispose();

            hbaoBuffer.Dispose();
            directionalLight.Dispose();

            perlinNoiseWidget.Release();
        }
    }
}