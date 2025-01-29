namespace VoxelEngine.Windows
{
    using System.Diagnostics;
    using Hexa.NET.D3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Input.Events;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Core.Windows.Events;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;
    using VoxelEngine.UI;

    public class GameWindow : CoreWindow
    {
        private bool firstFrame;
        private SwapChain swapChain;

        private readonly Scene scene;
        private Dispatcher renderDispatcher;
        private ImGuiManager renderer;
        private DebugDrawD3D11Renderer debugDraw;
        private bool resize;

        public GameWindow(Scene scene) : base(1920, 1080)
        {
            this.scene = scene;
        }

        public Dispatcher RenderDispatcher => renderDispatcher;

        public SwapChain SwapChain => swapChain;

        protected override void OnShown(ShownEventArgs args)
        {
            base.OnShown(args);
        }

        public override void RendererCreate()
        {
            DXGIDeviceManager.Initialize();

            swapChain = DXGIDeviceManager.CreateSwapChain(this);

            renderDispatcher = Dispatcher.CurrentDispatcher;
            renderer = new(this, D3D11DeviceManager.Device.As<ID3D11Device>(), D3D11DeviceManager.Context.As<ID3D11DeviceContext>());
            debugDraw = new(D3D11DeviceManager.Device.As<ID3D11Device>(), D3D11DeviceManager.Context.As<ID3D11DeviceContext>());
            SceneManager.SceneChanged += (_, _) => { firstFrame = true; };
            SceneManager.Load(scene);
        }

        public override void Render()
        {
            if (resize)
            {
                swapChain.Resize(Width, Height);
                SceneManager.Current?.Renderer.Resize(this);
                resize = false;
            }
            debugDraw.BeginDraw();
            renderer.NewFrame();

            Dispatcher.ExecuteQueue();
            lock (SceneManager.Current)
            {
                if (firstFrame)
                {
                    Time.Initialize();
                    firstFrame = false;
                }
                SceneManager.Current?.Render();
            }

            ImGuiConsole.Draw();

            swapChain.SetTarget(D3D11DeviceManager.Context.As<ID3D11DeviceContext>(), false);
            DebugDraw.SetViewport(new(Width, Height));
            debugDraw.EndDraw(swapChain.RTV, swapChain.DSV);
            renderer.EndFrame();

            swapChain.Present(Nucleus.Settings.VSync ? 1u : 0u);
        }

        public override void RendererDestroy()
        {
            renderer.Dispose();
            SceneManager.Unload();
            DXGIDeviceManager.Dispose();
        }

        private long fpsFrameCount;
        private long fpsStartTime;

        private void LimitFrameRate()
        {
            if (Nucleus.Settings.LimitFPS & !Nucleus.Settings.VSync)
            {
                int fps = Nucleus.Settings.TargetFPS;
                long freq = Stopwatch.Frequency;
                long frame = Stopwatch.GetTimestamp();
                while ((frame - fpsStartTime) * fps < freq * fpsFrameCount)
                {
                    int sleepTime = (int)((fpsStartTime * fps + freq * fpsFrameCount - frame * fps) * 1000 / (freq * fps));
                    if (sleepTime > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }

                    frame = Stopwatch.GetTimestamp();
                }
                if (++fpsFrameCount > fps)
                {
                    fpsFrameCount = 0;
                    fpsStartTime = frame;
                }
            }
        }

        protected override void OnResized(ResizedEventArgs args)
        {
            base.OnResized(args);
            resize = true;
        }

        protected override void OnClose(CloseEventArgs args)
        {
            Trace.WriteLine("Perfoming Shutdown");
            base.OnClose(args);
        }

        protected override void OnKeyboardInput(KeyboardEventArgs args)
        {
            base.OnKeyboardInput(args);
            if (args.KeyCode == Key.F5)
            {
                renderDispatcher.Invoke(GraphicsPipeline.ReloadShaders);
            }
            if (args.KeyCode == Key.F9)
            {
                Trace.WriteLine(Process.GetCurrentProcess().PrivateMemorySize64);
            }
        }
    }
}