using HexaEngine.Extensions;
using HexaEngine.Resources;
using HexaEngine.Scenes;
using HexaEngine.Scenes.Objects;
using HexaEngine.Windows.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Threading;
using Vortice.DXGI;
using static HexaEngine.Windows.Native.Helper;

namespace HexaEngine.Windows
{
    public abstract partial class RenderWindow : NativeWindow, System.IDisposable, IRenderSurface
    {
        private Thread renderThread;
        private bool disposedValue;
        private bool disposing;
        private bool first;

        public bool Focus { get; set; }

        public bool Fullscreen { get; set; } = false;

        public bool Resizeable { get; set; } = true;

        public Color BackgroundClear { get; set; } = Color.White;

        public DeviceManager DeviceManager { get; private set; }

        public Scene Scene { get; set; }

        public bool VSync { get; set; }

        public bool FPSLimit { get; set; }

        public virtual int ViewOffsetX => 0;

        public virtual int ViewOffsetY => 0;

        public virtual int ViewWidth { get; set; }

        public virtual int ViewHeight { get; set; }

        public RenderWindow(string title, int width, int height)
        {
            Title = title;
            Width = ViewWidth = width;
            Height = ViewHeight = height;
            Style = WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CLIPSIBLINGS;
            StyleEx = WindowExStyles.WS_EX_APPWINDOW | WindowExStyles.WS_EX_WINDOWEDGE;
        }

        ~RenderWindow()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: false);
        }

        protected override void OnHandleCreated()
        {
            DeviceManager = new DeviceManager(this) { Window = this };
            Scene = new(this, DeviceManager);
            renderThread = new Thread(TickInternal);
            renderThread.Start();
        }

        protected override IntPtr ProcessWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (uint)WindowMessage.ActivateApp)
            {
                Focus = IntPtrToInt32(wParam) != 0;
                if (Focus)
                {
                    OnActivated();
                }
                else
                {
                    OnDeactivated();
                }

                return base.ProcessWindowMessage(hWnd, msg, wParam, lParam);
            }

            if (msg == 0x0232)
            {
                DeviceManager.Resize(ViewWidth, ViewHeight);
            }

            return base.ProcessWindowMessage(hWnd, msg, wParam, lParam);
        }

        public DialogResult ShowMessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            uint style = (uint)((int)buttons | (int)icon | 0 | 0);
            return (DialogResult)User32.MessageBox(Handle, title, text, style);
        }

        public event EventHandler<EventArgs> Initialized;

        public event EventHandler<EventArgs> Uninitialized;

        public bool IsInitialized { get; set; }

        public bool IsRendering { get; set; } = true;

        public bool IsSimulating { get; set; } = true;

        protected abstract void InitializeComponent();

        protected virtual void EndRender()
        {
            DeviceManager.SwapChain.Present(VSync ? 1 : 0, PresentFlags.None);
        }

        protected override void OnResize()
        {
            base.OnResize();
            DeviceManager?.Resize(Width, Height);
        }

        internal void TickInternal()
        {
            while (!disposing)
            {
                while (!IsRendering)
                {
                    Time.FrameUpdate();
                    Thread.Sleep(1);
                }

                if (!first)
                {
                    Initialized?.Invoke(this, null);
                    first = true;
                    InitializeComponent();
                    Scene.Initialize();
                }

                Scene.Render();
                EndRender();
                LimitFrameRate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                this.disposing = true;
                while (renderThread.IsAlive) Thread.Sleep(1);
                Uninitialized?.Invoke(this, null);
                DeviceManager.ID3D11DeviceContext.Flush();
                DeviceManager.ID3D11DeviceContext.ClearState();
                Scene.Unload();
                ResourceManager.ReleaseAll();
                DeviceManager.Dispose();
                Close();
                disposedValue = true;
            }
        }

        public int FPSTarget = 60;
        private long fpsFrameCount;
        private long fpsStartTime;
        private readonly List<Camera> cameras = new();

        private void LimitFrameRate()
        {
            if (FPSLimit & !VSync)
            {
                int fps = FPSTarget;
                long freq = Stopwatch.Frequency;
                long frame = Stopwatch.GetTimestamp();
                while ((frame - fpsStartTime) * fps < freq * fpsFrameCount)
                {
                    int sleepTime = (int)((fpsStartTime * fps + freq * fpsFrameCount - frame * fps) * 1000 / (freq * fps));
                    if (sleepTime > 0) Thread.Sleep(sleepTime);
                    frame = Stopwatch.GetTimestamp();
                }
                if (++fpsFrameCount > fps)
                {
                    fpsFrameCount = 0;
                    fpsStartTime = frame;
                }
            }
        }
    }
}