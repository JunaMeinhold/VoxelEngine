namespace App.Renderers
{
    using App.Graphics.Graph;
    using App.Graphics.Passes;
    using App.Pipelines.Deferred;
    using Hexa.NET.DebugDraw;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.ImPlot;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Unsafes;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;
    using VoxelEngine.Windows;

    public class SceneRenderer : ISceneRenderer
    {
        private ConstantBuffer<CBCamera> cameraBuffer;

        private SwapChain swapChain;

        private SamplerState anisotropicClampSampler;
        private SamplerState pointClampSampler;
        private SamplerState linearClampSampler;

        private PerlinNoiseWidget perlinNoiseWidget;
        private WorldProfilerWidget profilerWidget = new();

        private int rendererWidth;
        private int rendererHeight;

        private readonly GraphResourceBuilder resourceBuilder = new();
        private List<RenderPass> passes = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(GameWindow window)
        {
            swapChain = window.SwapChain;
            rendererWidth = 2560;
            rendererHeight = 1440;

            anisotropicClampSampler = new(SamplerStateDescription.AnisotropicClamp);
            pointClampSampler = new(SamplerStateDescription.PointClamp);
            linearClampSampler = new(SamplerStateDescription.LinearClamp);

            D3D11GlobalResourceList.SetSampler("linearClampSampler", linearClampSampler);
            D3D11GlobalResourceList.SetSampler("pointClampSampler", pointClampSampler);
            D3D11GlobalResourceList.SetSampler("anisotropicClampSampler", anisotropicClampSampler);

            passes = new()
            {
                new LightUpdatePass(),
                new ShadowMapPass(),
                new DeferredGeometryPass(),
                new AOPass(),
                new DeferredLightPass(),
                new ForwardLightPass(),
                new PostProcessingPass(),
                new OverlayPass()
            };

            resourceBuilder.Viewport = new(rendererWidth, rendererHeight);
            resourceBuilder.OutputViewport = swapChain.Viewport;
            resourceBuilder.Output = swapChain.RTV;
            cameraBuffer = resourceBuilder.CreateConstantBuffer<CBCamera>("CameraBuffer", CpuAccessFlags.Write).Value!;
            var depthStencil = resourceBuilder.CreateDepthStencilBuffer("DepthStencil", new(Format.D32Float, rendererWidth, rendererHeight, 1), ResourceCreationFlags.None).Value!;

            D3D11GlobalResourceList.SetCBV("CameraBuffer", cameraBuffer);
            D3D11GlobalResourceList.SetSRV("DepthTex", depthStencil);

            foreach (var pass in passes)
            {
                pass.Configure(resourceBuilder);
            }

            resourceBuilder.CreateResources();

            foreach (var pass in passes)
            {
                pass.Init(resourceBuilder);
            }

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
        }

        public void Resize(GameWindow window)
        {
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

            var directionalLight = scene.LightSystem.ActiveDirectionalLight!;

            ImGui.InputFloat("Light Bleeding", ref directionalLight.LightBleedingReduction);

            DebugDraw.SetCamera(camera.Transform.ViewProjection);

            ImGui.InputFloat("LightDistanceFactor", ref directionalLight.Config.LightDistanceFactor);
            ImGui.InputFloat("FarFactor", ref directionalLight.Config.FarFactor);
            ImGui.InputFloat("PixelSnap", ref directionalLight.Config.PixelSnap);
            ImGui.Checkbox("Stabilize", ref directionalLight.Config.Stabilize);

            cameraBuffer.Update(context, new CBCamera(camera, new Vector2(rendererWidth, rendererHeight)));

            resourceBuilder.OutputViewport = swapChain.Viewport;
            resourceBuilder.Output = swapChain.RTV;

            foreach (var pass in passes)
            {
                pass.Execute(context, scene, camera, resourceBuilder);
            }
        }

        public void Dispose()
        {
            foreach (var pass in passes)
            {
                pass.Dispose();
            }

            resourceBuilder.ReleaseResources();
        }
    }
}