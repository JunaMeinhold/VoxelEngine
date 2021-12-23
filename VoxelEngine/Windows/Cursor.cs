using HexaEngine.Windows.Native;
using System;

namespace HexaEngine.Windows
{
    public static class Cursor
    {
        private static NativeWindow window;
        private static bool captureCursor;
        private static bool visible;

        public static void Initialize(NativeWindow window_)
        {
            window = window_;
            window.Resized += Window_Resized;
            window.Activated += Window_Activated;
            window.Deactivated += Window_Deactivated;
            window.HandleDestroy += Window_HandleDestroy;
        }

        private static void Window_Resized(object sender, EventArgs e)
        {
            UpdateCursorState();
        }

        private static void Window_HandleDestroy(object sender, EventArgs e)
        {
            if (!visible)
                _ = User32.ShowCursor(true);
            if (captureCursor)
                User32.LockCursor(IntPtr.Zero);
        }

        private static void Window_Deactivated(object sender, EventArgs e)
        {
            if (!visible)
                _ = User32.ShowCursor(true);
            if (captureCursor)
                User32.LockCursor(IntPtr.Zero);
        }

        private static void Window_Activated(object sender, EventArgs e)
        {
            _ = User32.ShowCursor(visible);
            if (captureCursor)
                User32.LockCursor(window.Handle);
        }

        public static bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                UpdateCursorState();
            }
        }

        public static bool Capture
        {
            get => captureCursor;
            set
            {
                captureCursor = value;
                UpdateCursorState();
            }
        }

        private static void UpdateCursorState()
        {
            if (captureCursor & window.ParentHandle != IntPtr.Zero)
            {
                User32.LockCursor(window.ParentHandle);
            }
            else if (captureCursor & window.IsActive)
            {
                User32.LockCursor(window.Handle);
            }
            else if (window.IsActive)
            {
                User32.LockCursor(IntPtr.Zero);
            }
            if (visible & window.IsActive)
            {
                _ = User32.ShowCursor(true);
            }
            else if (window.IsActive)
            {
                _ = User32.ShowCursor(false);
            }
        }
    }
}