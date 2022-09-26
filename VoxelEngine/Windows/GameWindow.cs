namespace VoxelEngine.Windows
{
    using System.Diagnostics;
    using VoxelEngine;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Events;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Input.Events;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Rendering.DXGI;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Scenes;
    using VoxelEngine.UI;

    public class GameWindow : Window
    {
        private Thread renderThread;
        private bool isRunning = true;
        private bool firstFrame;
        private readonly Scene scene;
        private Dispatcher renderDispatcher;
        private ImGuiRenderer renderer;
        private bool resize;

        public GameWindow(Scene scene)
        {
            Width = 1280;
            Height = 720;
            this.scene = scene;
        }

        public Dispatcher RenderDispatcher => renderDispatcher;

        protected override void OnShown(ShownEventArgs args)
        {
            base.OnShown(args);

            renderThread = new(RenderVoid);
            renderThread.Name = "RenderThread";
            renderThread.Start();
        }

        [STAThread]
        private void RenderVoid()
        {
            DXGIDeviceManager.Initialize(this);

            renderDispatcher = Dispatcher.CurrentDispatcher;
            renderer = new(this);
            SceneManager.SceneChanged += (_, _) => { firstFrame = true; };
            SceneManager.Load(scene);
            Time.Initialize();

            while (isRunning)
            {
                if (resize)
                {
                    DXGIDeviceManager.Resize(Width, Height);
                    SceneManager.Current.Renderer.Resize(D3D11DeviceManager.ID3D11Device, this);
                    resize = false;
                }
                renderer.BeginDraw();
                Time.FrameUpdate();
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

                renderer.EndDraw();
                DXGIDeviceManager.SwapChain.Present(Nucleus.Settings.VSync ? 1 : 0);
                LimitFrameRate();
                Keyboard.FrameUpdate();
            }

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
            isRunning = false;
            renderThread.Join();

            Trace.WriteLine("Perfoming Shutdown");
            base.OnClose(args);
        }

        protected override void OnKeyboardInput(KeyboardEventArgs args)
        {
            base.OnKeyboardInput(args);
            if (args.KeyCode == Silk.NET.SDL.KeyCode.KF5)
            {
                renderDispatcher.Invoke(() => Pipeline.ReloadShaders());
            }
            if (args.KeyCode == Silk.NET.SDL.KeyCode.KF9)
            {
                Trace.WriteLine(Process.GetCurrentProcess().PrivateMemorySize64);
            }
        }
    }
}