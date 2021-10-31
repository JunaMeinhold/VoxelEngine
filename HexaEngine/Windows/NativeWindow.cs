using HexaEngine.Input;
using HexaEngine.Input.Events;
using HexaEngine.Input.RawInput;
using HexaEngine.Input.RawInput.Events;
using HexaEngine.Windows.Native;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static HexaEngine.Windows.Native.Helper;

namespace HexaEngine.Windows
{
    public class NativeWindow : IDisposable
    {
        #region Fields

        private WNDPROC WNDPROC;
        private string title;
        private int x;
        private int y;
        private int width;
        private int height;
        private readonly List<HidUsageAndPage> registeredDevices = new();
        private bool disposedValue;
        private bool borderlessFullscreen;

        #endregion Fields

        #region Properties

        public string Title
        {
            get => title;
            set
            {
                title = value;
                _ = User32.SetWindowTextA(Handle, value);
            }
        }

        public int X { get => x; protected set { x = value; _ = User32.MoveWindow(Handle, x, y, width, height, true); } }

        public int Y { get => y; protected set { y = value; _ = User32.MoveWindow(Handle, x, y, width, height, true); } }

        public int Width { get => width; protected set { width = value; _ = User32.MoveWindow(Handle, x, y, width, height, true); } }

        public int Height { get => height; protected set { height = value; _ = User32.MoveWindow(Handle, x, y, width, height, true); } }

        public bool BorderlessFullscreen { get => borderlessFullscreen; set => borderlessFullscreen = value; }

        public WindowStyles Style { get; set; }

        public WindowExStyles StyleEx { get; set; }

        public StartupLocation StartupLocation { get; set; }

        public IntPtr Handle { get; private set; }

        public IntPtr ParentHandle { get; private set; }

        public bool IsDisposed { get => disposedValue; }

        public bool IsActive { get; private set; }

        public bool IsShown { get; private set; }

        protected bool SuppressLegacyInput { get; set; }

        protected bool SuppressMouseMoveInputEvents { get; set; }

        #endregion Properties

        #region Constructors and Destructors

        public NativeWindow()
        {
        }

        ~NativeWindow()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: false);
        }

        #endregion Constructors and Destructors

        #region Window construction

        private void PlatformConstruct()
        {
            WNDPROC = ProcessWindowMessage;
            var wndClassEx = new WNDCLASSEX
            {
                Size = Unsafe.SizeOf<WNDCLASSEX>(),
                Styles = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_OWNDC,
                WindowProc = WNDPROC,
                InstanceHandle = Kernel32.GetModuleHandle(null),
                CursorHandle = User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = IntPtr.Zero,
                IconHandle = IntPtr.Zero,
                ClassName = Title + "Window",
            };

            var atom = User32.RegisterClassEx(ref wndClassEx);
        }

        protected void CreateWindow(IntPtr parent)
        {
            ParentHandle = parent;
            PlatformConstruct();
            int windowWidth;
            int windowHeight;

            if (Width > 0 && Height > 0)
            {
                Rect rect = new Rect(0, 0, Width, Height);

                // Adjust according to window styles
                _ = User32.AdjustWindowRectEx(
                    ref rect,
                    Style,
                    false,
                    StyleEx);

                windowWidth = rect.Right - rect.Left;
                windowHeight = rect.Bottom - rect.Top;
            }
            else
            {
                X = Y = windowWidth = windowHeight = unchecked((int)0x80000000);
            }

            switch (StartupLocation)
            {
                case StartupLocation.TopLeft:

                    break;

                case StartupLocation.Center:

                    X = (User32.GetSystemMetrics(SystemMetrics.SM_CXSCREEN) - windowWidth) / 2;
                    Y = (User32.GetSystemMetrics(SystemMetrics.SM_CYSCREEN) - windowHeight) / 2;

                    break;
            }

            var hwnd = User32.CreateWindowEx(
                (int)StyleEx,
                Title + "Window",
                Title,
                (int)Style,
                X,
                Y,
                windowWidth,
                windowHeight,
                parent,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hwnd == IntPtr.Zero)
            {
                return;
            }
            Handle = hwnd;
            Cursor.Initialize(this);
            Keyboard.Initialize();
            Mouse.Initialize();
            OnHandleCreated();
        }

        #endregion Window construction

        #region Window message loop

        protected virtual IntPtr ProcessWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((WindowMessage)msg)
            {
                case WindowMessage.ActivateApp:
                    IsActive = IntPtrToInt32(wParam) != 0;
                    if (IsActive)
                    {
                        OnActivated();
                    }
                    else
                    {
                        OnDeactivated();
                    }

                    break;

                case WindowMessage.Destroy:
                    OnClose();
                    Close();
                    break;

                case WindowMessage.Size:
                    width = SignedLOWORD(lParam);
                    height = SignedHIWORD(lParam);
                    OnResize();
                    break;

                case WindowMessage.Move:
                    x = SignedLOWORD(lParam);
                    y = SignedHIWORD(lParam);
                    OnMove();
                    break;

                case WindowMessage.MouseMove:
                    if (!Mouse.Hover)
                    {
                        OnMouseEnter(Mouse.Update(true));
                        var tme = new TRACKMOUSEEVENT
                        {
                            cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
                            dwFlags = TMEFlags.TME_LEAVE,
                            hWnd = Handle
                        };
                        _ = User32.TrackMouseEvent(ref tme);
                    }
                    OnMouseMove(Mouse.Update(MakePoint(lParam)));
                    break;

                case WindowMessage.MouseLeave:
                    OnMouseLeave(Mouse.Update(false));
                    break;

                case WindowMessage.MouseWheel:
                    OnMouseWheel(new MouseWheelEventArgs((short)SignedHIWORD(wParam)));
                    break;
            }

            if (msg != 0x00FF)
            {
                return User32.DefWindowProc(hWnd, msg, wParam, lParam);
            }
            // Create an RawInputData from the handle stored in lParam.
            var data = RawInputData.FromHandle(lParam);

            switch (data)
            {
                case RawInputMouseData mouse:
                    {
                        var args = Mouse.Update(mouse);
                        MouseInput?.Invoke(this, args);
                        if (args.Key == MouseButton.None)
                        {
                            OnMouseMoveRelative(args);
                        }
                        else
                        {
                            if (args.IsDown)
                            {
                                OnMouseDown(args);
                            }
                            else
                            {
                                OnMouseUp(args);
                            }
                        }
                    }
                    break;

                case RawInputKeyboardData keyboard:
                    {
                        var args = Keyboard.Update(keyboard);
                        KeyboardInput?.Invoke(this, args);
                        if (args.IsDown)
                        {
                            OnKeyDown(args);
                        }
                        else
                        {
                            OnKeyUp(args);
                        }
                    }
                    break;

                case RawInputHidData hid:
                    HIDInput?.Invoke(this, new HIDRawInputEventArgs(hid));
                    break;
            }

            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        #endregion Window message loop

        #region Methodes

        public void Show()
        {
            if (Handle == IntPtr.Zero)
            {
                CreateWindow(IntPtr.Zero);
            }

            _ = User32.ShowWindow(Handle, ShowWindowCommand.Normal);
            IsShown = true;
        }

        public void Show(IntPtr parent)
        {
            if (Handle == IntPtr.Zero)
            {
                CreateWindow(parent);
            }

            _ = User32.ShowWindow(Handle, ShowWindowCommand.ShowDefault);
            IsShown = true;
        }

        public void Close()
        {
            var hwnd = Handle;
            if (hwnd != IntPtr.Zero)
            {
                var destroyHandle = hwnd;
                Handle = IntPtr.Zero;

                OnHandleDestroy();
                _ = User32.DestroyWindow(destroyHandle);
                _ = User32.UnregisterClass(Title + "Window", destroyHandle);
                IsShown = false;
            }
        }

        public void RegisterControl(HidUsageAndPage device, RawInputDeviceFlags flags = RawInputDeviceFlags.None)
        {
            if (!registeredDevices.Contains(device))
            {
                RawInputDevice.RegisterDevice(device, flags, Handle);
                registeredDevices.Add(device);
            }
        }

        public void UnregisterControl(HidUsageAndPage device)
        {
            if (registeredDevices.Contains(device))
            {
                RawInputDevice.UnregisterDevice(device);
                _ = registeredDevices.Remove(device);
            }
        }

        public void SetBorderlessFullscreen(bool state)
        {
            if (state)
            {
                var cx = User32.GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
                var cy = User32.GetSystemMetrics(SystemMetrics.SM_CYSCREEN);
                _ = User32.SetWindowLongPtr(Handle, WindowLongParam.GWL_STYLE, (long)((WindowStyles)User32.GetWindowLongPtr(Handle, WindowLongParam.GWL_STYLE) & (WindowStyles.WS_THICKFRAME | WindowStyles.WS_BORDER | WindowStyles.WS_DLGFRAME | WindowStyles.WS_CAPTION | WindowStyles.WS_MINIMIZEBOX | WindowStyles.WS_MAXIMIZEBOX | WindowStyles.WS_SYSMENU)));
                _ = User32.SetWindowPos(Handle, 0, 0, cx, cy, SetWindowPosFlags.NOMOVE | SetWindowPosFlags.NOSIZE);
            }
        }

        #endregion Methodes

        #region Events

        public event EventHandler<KeyboardRawInputEventArgs> KeyboardInput;

        public event EventHandler<MouseRawInputEventArgs> MouseInput;

        public event EventHandler<HIDRawInputEventArgs> HIDInput;

        public event EventHandler<EventArgs> HandleCreated;

        public event EventHandler<EventArgs> HandleDestroy;

        public event EventHandler<EventArgs> Activated;

        public event EventHandler<EventArgs> Deactivated;

        public event EventHandler<MouseEventArgs> MouseMove;

        public event EventHandler<MouseEventArgs> MouseMoveRelative;

        public event EventHandler<KeyboardEventArgs> KeyDown;

        public event EventHandler<KeyboardEventArgs> KeyUp;

        public event EventHandler<EventArgs> Resized;

        protected virtual void OnActivated()
        {
            Activated?.Invoke(this, null);
        }

        protected virtual void OnDeactivated()
        {
            Deactivated?.Invoke(this, null);
        }

        protected virtual void OnHandleCreated()
        {
            HandleCreated?.Invoke(this, null);
        }

        protected virtual void OnHandleDestroy()
        {
            HandleDestroy?.Invoke(this, null);
        }

        protected virtual void OnResize()
        {
            Resized?.Invoke(this, null);
        }

        protected virtual void OnMove()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnMouseEnter(MouseEventArgs mouseEventArgs)
        {
        }

        protected virtual void OnMouseMove(MouseEventArgs mouseEventArgs)
        {
            MouseMove?.Invoke(this, mouseEventArgs);
        }

        protected virtual void OnMouseMoveRelative(MouseEventArgs mouseEventArgs)
        {
            MouseMoveRelative?.Invoke(this, mouseEventArgs);
        }

        protected virtual void OnMouseLeave(MouseEventArgs mouseEventArgs)
        {
        }

        protected virtual void OnMouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
        }

        protected virtual void OnMouseDown(MouseEventArgs mouseEventArgs)
        {
        }

        protected virtual void OnMouseUp(MouseEventArgs mouseEventArgs)
        {
        }

        protected virtual void OnKeyDown(KeyboardEventArgs keyboardEventArgs)
        {
            KeyDown?.Invoke(this, keyboardEventArgs);
        }

        protected virtual void OnKeyUp(KeyboardEventArgs keyboardEventArgs)
        {
            KeyUp?.Invoke(this, keyboardEventArgs);
        }

        #endregion Events

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (HidUsageAndPage device in registeredDevices)
                {
                    RawInputDevice.UnregisterDevice(device);
                }
                Close();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Dispose
    }
}