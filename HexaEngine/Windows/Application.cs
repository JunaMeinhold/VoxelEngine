using HexaEngine.Logging;
using HexaEngine.Windows.Native;
using System;
using System.Diagnostics;

namespace HexaEngine.Windows
{
    public static class Application
    {
        private static bool _exitRequested;

        public static NativeWindow MainWindow { get; private set; }

        public static event EventHandler ApplicationClosing;

        public static void Run(NativeWindow window)
        {
            _ = Trace.Listeners.Add(new DebugListener($"logs/{DateTime.Now.ToFileTime()}-log.txt"));

            MainWindow = window;
            window.Show();
            PlatformRun();
        }

        public static void SetHighDpiMode(HighDpiMode mode)
        {
            if (mode == HighDpiMode.SystemAware)
                _ = User32.SetProcessDPIAware();
        }

        public static void Exit()
        {
            User32.PostQuitMessage(0);
        }

        private static void PlatformRun()
        {
#if DEBUG
            Trace.WriteLine("Starting native message loop");
#endif
            while (!_exitRequested)
            {
                if (MainWindow.IsShown)
                {
                    var ret = User32.GetMessage(out var msg, IntPtr.Zero, 0, 0);
                    if (ret == 0)
                    {
                        _exitRequested = true;
                        break;
                    }
                    else if (ret == -1)
                    {
                        _exitRequested = true;
                        break;
                    }
                    else
                    {
                        User32.TranslateMessage(ref msg);
                        User32.DispatchMessage(ref msg);

                        if (msg.Value == (uint)WindowMessage.Quit)
                        {
                            _exitRequested = true;
                            break;
                        }
                    }
                }
                else
                {
                    _exitRequested = true;
                }
            }
            MainWindow.Dispose();
            ApplicationClosing?.Invoke(null, null);
        }
    }
}