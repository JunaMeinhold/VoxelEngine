using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace HexaEngine.Windows.Native
{
#pragma warning disable CA1069 // Enumerationswerte dürfen nicht dupliziert werden

    #region Enums

    public enum WindowLongParam
    {
        /// <summary>Sets a new address for the window procedure.</summary>
        /// <remarks>You cannot change this attribute if the window does not belong to the same process as the calling thread.</remarks>
        GWL_WNDPROC = -4,

        /// <summary>Sets a new application instance handle.</summary>
        GWLP_HINSTANCE = -6,

        GWLP_HWNDPARENT = -8,

        /// <summary>Sets a new identifier of the child window.</summary>
        /// <remarks>The window cannot be a top-level window.</remarks>
        GWL_ID = -12,

        /// <summary>Sets a new window style.</summary>
        GWL_STYLE = -16,

        /// <summary>Sets a new extended window style.</summary>
        /// <remarks>See <see cref="ExWindowStyles"/>.</remarks>
        GWL_EXSTYLE = -20,

        /// <summary>Sets the user data associated with the window.</summary>
        /// <remarks>This data is intended for use by the application that created the window. Its value is initially zero.</remarks>
        GWL_USERDATA = -21,

        /// <summary>Sets the return value of a message processed in the dialog box procedure.</summary>
        /// <remarks>Only applies to dialog boxes.</remarks>
        DWLP_MSGRESULT = 0,

        /// <summary>Sets new extra information that is private to the application, such as handles or pointers.</summary>
        /// <remarks>Only applies to dialog boxes.</remarks>
        DWLP_USER = 8,

        /// <summary>Sets the new address of the dialog box procedure.</summary>
        /// <remarks>Only applies to dialog boxes.</remarks>
        DWLP_DLGPROC = 4
    }

    public enum SetWindowPosFlags
    {
        NOSIZE = 0x0001,
        NOMOVE = 0x0002,
        NOZORDER = 0x0004,
        NOREDRAW = 0x0008,
        NOACTIVATE = 0x0010,
        DRAWFRAME = 0x0020,
        FRAMECHANGED = 0x0020,
        SHOWWINDOW = 0x0040,
        HIDEWINDOW = 0x0080,
        NOCOPYBITS = 0x0100,
        NOOWNERZORDER = 0x0200,
        NOREPOSITION = 0x0200,
        NOSENDCHANGING = 0x0400,
        DEFERERASE = 0x2000,
        ASYNCWINDOWPOS = 0x4000
    }

    public enum WCmd : uint
    {
        GW_HWNDFIRST = 0,
        GW_HWNDLAST = 1,
        GW_HWNDNEXT = 2,
        GW_HWNDPREV = 3,
        GW_OWNER = 4,
        GW_CHILD = 5,
        GW_ENABLEDPOPUP = 6,
        GW_MAX = 6,
    }

    [Flags]
    public enum WindowStyles
    {
        WS_BORDER = 0x00800000,
        WS_CAPTION = 0x00C00000,
        WS_CHILD = 0x40000000,
        WS_CHILDWINDOW = 0x40000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_DISABLED = 0x08000000,
        WS_DLGFRAME = 0x00400000,
        WS_GROUP = 0x00020000,
        WS_HSCROLL = 0x00100000,
        WS_ICONIC = 0x20000000,
        WS_MAXIMIZE = 0x01000000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_MINIMIZE = 0x20000000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_EX_APPWINDOW = 0x00040000,
        WS_OVERLAPPED = 0x00000000,

        WS_OVERLAPPEDWINDOW =
            WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

        WS_POPUP = unchecked((int)0x80000000),
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        WS_SIZEBOX = 0x00040000,
        WS_SYSMENU = 0x00080000,
        WS_TABSTOP = 0x00010000,
        WS_THICKFRAME = 0x00040000,
        WS_TILED = 0x00000000,
        WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_VISIBLE = 0x10000000,
        WS_VSCROLL = 0x00200000,
    }

    [Flags]
    public enum WindowExStyles : uint
    {
        WS_EX_LEFT = 0x00000000,
        WS_EX_LTRREADING = 0x00000000,
        WS_EX_RIGHTSCROLLBAR = 0x00000000,
        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_ACCEPTFILES = 0x00000010,
        WS_EX_TRANSPARENT = 0x00000020,
        WS_EX_MDICHILD = 0x00000040,

        /// <summary>
        ///     The window is intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a
        ///     normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar
        ///     or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not
        ///     displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE.
        /// </summary>
        WS_EX_TOOLWINDOW = 0x00000080,

        WS_EX_WINDOWEDGE = 0x00000100,
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        WS_EX_CONTEXTHELP = 0x00000400,
        WS_EX_RIGHT = 0x00001000,
        WS_EX_RTLREADING = 0x00002000,
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        WS_EX_CONTROLPARENT = 0x00010000,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_LAYERED = 0x00080000,
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
        WS_EX_LAYOUTRTL = 0x00400000,
        WS_EX_COMPOSITED = 0x02000000,
        WS_EX_NOACTIVATE = 0x08000000
    }

    [Flags]
    public enum WindowClassStyles
    {
        CS_BYTEALIGNCLIENT = 0x1000,
        CS_BYTEALIGNWINDOW = 0x2000,
        CS_CLASSDC = 0x0040,
        CS_DBLCLKS = 0x0008,
        CS_DROPSHADOW = 0x00020000,
        CS_GLOBALCLASS = 0x4000,
        CS_HREDRAW = 0x0002,
        CS_NOCLOSE = 0x0200,
        CS_OWNDC = 0x0020,
        CS_PARENTDC = 0x0080,
        CS_SAVEBITS = 0x0800,
        CS_VREDRAW = 0x0001
    }

    public enum WindowMessage : uint
    {
        Null = 0x0000,
        Create = 0x0001,
        Destroy = 0x0002,
        Move = 0x0003,
        Size = 0x0005,
        Activate = 0x0006,
        SetFocus = 0x0007,
        KillFocus = 0x0008,
        Enable = 0x000A,
        SetRedraw = 0x000B,
        SetText = 0x000C,
        GetText = 0x000D,
        GetTextLength = 0x000E,
        Paint = 0x000F,
        Close = 0x0010,
        QueryEndSession = 0x0011,
        QueryOpen = 0x0013,
        EndSession = 0x0016,
        Quit = 0x0012,
        EraseBackground = 0x0014,
        SystemColorChange = 0x0015,
        ShowWindow = 0x0018,
        WindowsIniChange = 0x001A,
        SettingChange = WindowsIniChange,
        DevModeChange = 0x001B,
        ActivateApp = 0x001C,
        FontChange = 0x001D,
        TimeChange = 0x001E,
        CancelMode = 0x001F,
        SetCursor = 0x0020,
        MouseActivate = 0x0021,
        ChildActivate = 0x0022,
        KeyDown = 0x0100,
        KeyUp = 0x0101,
        Char = 0x0102,
        SysKeyDown = 0x0104,
        SysKeyUp = 0x0105,
        MouseMove = 0x0200,
        LButtonDown = 0x0201,
        LButtonUp = 0x0202,
        MButtonDown = 0x0207,
        MButtonUp = 0x0208,
        RButtonDown = 0x0204,
        RButtonUp = 0x0205,
        MouseWheel = 0x020A,
        XButtonDown = 0x020B,
        XButtonUp = 0x020C,
        MouseLeave = 0x02A3,
        NcMouseMove = 0x00A0,
        WindowPositionChanging = 0x0046,
        WindowPositionChanged = 0x0047,
        LButtonDBLCLK = 0x0203,
    }

    public enum ShowWindowCommand
    {
        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,

        /// <summary>
        /// Activates and displays a window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when displaying the window
        /// for the first time.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        Maximize = 3,

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = 3,

        /// <summary>
        /// Displays a window in its most recent size and position. This value
        /// is similar to <see cref="ShowWindowCommand.Normal"/>, except
        /// the window is not activated.
        /// </summary>
        ShowNoActivate = 4,

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,

        /// <summary>
        /// Minimizes the specified window and activates the next top-level
        /// window in the Z order.
        /// </summary>
        Minimize = 6,

        /// <summary>
        /// Displays the window as a minimized window. This value is similar to
        /// <see cref="ShowMinimized"/>, except the
        /// window is not activated.
        /// </summary>
        ShowMinNoActive = 7,

        /// <summary>
        /// Displays the window in its current size and position. This value is
        /// similar to <see cref="Show"/>, except the
        /// window is not activated.
        /// </summary>
        ShowNA = 8,

        /// <summary>
        /// Activates and displays the window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,

        /// <summary>
        /// Sets the show state based on the SW_* value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.
        /// </summary>
        ShowDefault = 10,

        /// <summary>
        ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
        /// that owns the window is not responding. This flag should only be
        /// used when minimizing windows from a different thread.
        /// </summary>
        ForceMinimize = 11
    }

    public enum SystemCursor
    {
        IDC_ARROW = 32512,
        IDC_IBEAM = 32513,
        IDC_WAIT = 32514,
        IDC_CROSS = 32515,
        IDC_UPARROW = 32516,
        IDC_SIZE = 32640,
        IDC_ICON = 32641,
        IDC_SIZENWSE = 32642,
        IDC_SIZENESW = 32643,
        IDC_SIZEWE = 32644,
        IDC_SIZENS = 32645,
        IDC_SIZEALL = 32646,
        IDC_NO = 32648,
        IDC_HAND = 32649,
        IDC_APPSTARTING = 32650,
        IDC_HELP = 32651
    }

    public enum SystemMetrics
    {
        SM_CXSCREEN = 0,  // 0x00
        SM_CYSCREEN = 1,  // 0x01
        SM_CXVSCROLL = 2,  // 0x02
        SM_CYHSCROLL = 3,  // 0x03
        SM_CYCAPTION = 4,  // 0x04
        SM_CXBORDER = 5,  // 0x05
        SM_CYBORDER = 6,  // 0x06
        SM_CXDLGFRAME = 7,  // 0x07
        SM_CXFIXEDFRAME = 7,  // 0x07
        SM_CYDLGFRAME = 8,  // 0x08
        SM_CYFIXEDFRAME = 8,  // 0x08
        SM_CYVTHUMB = 9,  // 0x09
        SM_CXHTHUMB = 10, // 0x0A
        SM_CXICON = 11, // 0x0B
        SM_CYICON = 12, // 0x0C
        SM_CXCURSOR = 13, // 0x0D
        SM_CYCURSOR = 14, // 0x0E
        SM_CYMENU = 15, // 0x0F
        SM_CXFULLSCREEN = 16, // 0x10
        SM_CYFULLSCREEN = 17, // 0x11
        SM_CYKANJIWINDOW = 18, // 0x12
        SM_MOUSEPRESENT = 19, // 0x13
        SM_CYVSCROLL = 20, // 0x14
        SM_CXHSCROLL = 21, // 0x15
        SM_DEBUG = 22, // 0x16
        SM_SWAPBUTTON = 23, // 0x17
        SM_CXMIN = 28, // 0x1C
        SM_CYMIN = 29, // 0x1D
        SM_CXSIZE = 30, // 0x1E
        SM_CYSIZE = 31, // 0x1F
        SM_CXSIZEFRAME = 32, // 0x20
        SM_CXFRAME = 32, // 0x20
        SM_CYSIZEFRAME = 33, // 0x21
        SM_CYFRAME = 33, // 0x21
        SM_CXMINTRACK = 34, // 0x22
        SM_CYMINTRACK = 35, // 0x23
        SM_CXDOUBLECLK = 36, // 0x24
        SM_CYDOUBLECLK = 37, // 0x25
        SM_CXICONSPACING = 38, // 0x26
        SM_CYICONSPACING = 39, // 0x27
        SM_MENUDROPALIGNMENT = 40, // 0x28
        SM_PENWINDOWS = 41, // 0x29
        SM_DBCSENABLED = 42, // 0x2A
        SM_CMOUSEBUTTONS = 43, // 0x2B
        SM_SECURE = 44, // 0x2C
        SM_CXEDGE = 45, // 0x2D
        SM_CYEDGE = 46, // 0x2E
        SM_CXMINSPACING = 47, // 0x2F
        SM_CYMINSPACING = 48, // 0x30
        SM_CXSMICON = 49, // 0x31
        SM_CYSMICON = 50, // 0x32
        SM_CYSMCAPTION = 51, // 0x33
        SM_CXSMSIZE = 52, // 0x34
        SM_CYSMSIZE = 53, // 0x35
        SM_CXMENUSIZE = 54, // 0x36
        SM_CYMENUSIZE = 55, // 0x37
        SM_ARRANGE = 56, // 0x38
        SM_CXMINIMIZED = 57, // 0x39
        SM_CYMINIMIZED = 58, // 0x3A
        SM_CXMAXTRACK = 59, // 0x3B
        SM_CYMAXTRACK = 60, // 0x3C
        SM_CXMAXIMIZED = 61, // 0x3D
        SM_CYMAXIMIZED = 62, // 0x3E
        SM_NETWORK = 63, // 0x3F
        SM_CLEANBOOT = 67, // 0x43
        SM_CXDRAG = 68, // 0x44
        SM_CYDRAG = 69, // 0x45
        SM_SHOWSOUNDS = 70, // 0x46
        SM_CXMENUCHECK = 71, // 0x47
        SM_CYMENUCHECK = 72, // 0x48
        SM_SLOWMACHINE = 73, // 0x49
        SM_MIDEASTENABLED = 74, // 0x4A
        SM_MOUSEWHEELPRESENT = 75, // 0x4B
        SM_XVIRTUALSCREEN = 76,
        SM_YVIRTUALSCREEN = 77,
        SM_CXVIRTUALSCREEN = 78, // 0x4E
        SM_CYVIRTUALSCREEN = 79, // 0x4F
        SM_CMONITORS = 80, // 0x50
        SM_SAMEDISPLAYFORMAT = 81, // 0x51
        SM_IMMENABLED = 82, // 0x52
        SM_CXFOCUSBORDER = 83, // 0x53
        SM_CYFOCUSBORDER = 84, // 0x54
        SM_TABLETPC = 86,
        SM_MEDIACENTER = 87,
        SM_STARTER = 88,
        SM_SERVERR2 = 89,
        SM_MOUSEHORIZONTALWHEELPRESENT = 91,
        SM_CXPADDEDBORDER = 92,
        SM_DIGITIZER = 94,
        SM_MAXIMUMTOUCHES = 95,

        SM_REMOTESESSION = 0x1000,
        SM_SHUTTINGDOWN = 0x2000,
        SM_REMOTECONTROL = 0x2001,

        SM_CONVERTABLESLATEMODE = 0x2003,
        SM_SYSTEMDOCKED = 0x2004,
    }

    [Flags]
    public enum TMEFlags : uint
    {
        /// <summary>
        /// The caller wants to cancel a prior tracking request. The caller should also specify the type of tracking that it wants to cancel. For example, to cancel hover tracking, the caller must pass the TME_CANCEL and TME_HOVER flags.
        /// </summary>
        TME_CANCEL = 0x80000000,

        /// <summary>
        /// The caller wants hover notification. Notification is delivered as a WM_MOUSEHOVER message.
        /// If the caller requests hover tracking while hover tracking is already active, the hover timer will be reset.
        /// This flag is ignored if the mouse pointer is not over the specified window or area.
        /// </summary>
        TME_HOVER = 0x00000001,

        /// <summary>
        /// The caller wants leave notification. Notification is delivered as a WM_MOUSELEAVE message. If the mouse is not over the specified window or area, a leave notification is generated immediately and no further tracking is performed.
        /// </summary>
        TME_LEAVE = 0x00000002,

        /// <summary>
        /// The caller wants hover and leave notification for the nonclient areas. Notification is delivered as WM_NCMOUSEHOVER and WM_NCMOUSELEAVE messages.
        /// </summary>
        TME_NONCLIENT = 0x00000010,

        /// <summary>
        /// The function fills in the structure instead of treating it as a tracking request. The structure is filled such that had that structure been passed to TrackMouseEvent, it would generate the current tracking. The only anomaly is that the hover time-out returned is always the actual time-out and not HOVER_DEFAULT, if HOVER_DEFAULT was specified during the original TrackMouseEvent request.
        /// </summary>
        TME_QUERY = 0x40000000,
    }

    #endregion Enums

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACKMOUSEEVENT
    {
        public int cbSize;    // using Int32 instead of UInt32 is safe here, and this avoids casting the result  of Marshal.SizeOf()

        [MarshalAs(UnmanagedType.U4)]
        public TMEFlags dwFlags;

        public IntPtr hWnd;
        public uint dwHoverTime;

        public TRACKMOUSEEVENT(int dwFlags, IntPtr hWnd, uint dwHoverTime)
        {
            this.cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
            this.dwFlags = (TMEFlags)dwFlags;
            this.hWnd = hWnd;
            this.dwHoverTime = dwHoverTime;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public IntPtr Hwnd;
        public uint Value;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Point Point;
    }

    public delegate IntPtr WNDPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX
    {
        public int Size;
        public WindowClassStyles Styles;

        [MarshalAs(UnmanagedType.FunctionPtr)] public WNDPROC WindowProc;
        public int ClassExtraBytes;
        public int WindowExtraBytes;
        public IntPtr InstanceHandle;
        public IntPtr IconHandle;
        public IntPtr CursorHandle;
        public IntPtr BackgroundBrushHandle;
        public string MenuName;
        public string ClassName;
        public IntPtr SmallIconHandle;
    }

    #endregion Structures

    internal static class User32
    {
        public const string LibraryName = "user32.dll";

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName)]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr CallWindowProc(WNDPROC lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, string lpCursorName);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorResource);

        public static IntPtr LoadCursor(IntPtr hInstance, SystemCursor cursor)
        {
            return LoadCursor(hInstance, new IntPtr((int)cursor));
        }

        [DllImport(LibraryName)]
        public static extern IntPtr SetCursor(HandleRef hcursor);

        [DllImport(LibraryName)]
        public static extern int ShowCursor(bool state);

        [DllImport(LibraryName)]
        public static extern bool GetCursorPos(ref Point point);

        [DllImport(LibraryName)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetMessage(out Message lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "PeekMessageW")]
        public static extern bool PeekMessage(
            out Message lpMsg,
            IntPtr hWnd,
            uint wMsgFilterMin,
            uint wMsgFilterMax,
            uint wRemoveMsg);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "PostMessageW")]
        public static extern bool PostMessage(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool TranslateMessage([In] ref Message lpMsg);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr DispatchMessage([In] ref Message lpmsg);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetSystemMetrics(SystemMetrics smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong32b(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern long GetWindowLongPtr(IntPtr hWnd, WindowLongParam param);

        public static uint GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return GetWindowLong32b(hWnd, nIndex);
            }

            return GetWindowLongPtr(hWnd, nIndex);
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong32b(IntPtr hWnd, int nIndex, uint value);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SetWindowLongPtr(IntPtr hWnd, int nIndex, uint value);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern long SetWindowLongPtr(IntPtr hWnd, WindowLongParam param, long value);

        public static uint SetWindowLong(IntPtr hWnd, int nIndex, uint value)
        {
            if (IntPtr.Size == 4)
            {
                return SetWindowLong32b(hWnd, nIndex, value);
            }

            return SetWindowLongPtr(hWnd, nIndex, value);
        }

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AdjustWindowRect([In][Out] ref Rect lpRect, WindowStyles dwStyle, bool hasMenu);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AdjustWindowRectEx([In][Out] ref Rect lpRect, WindowStyles dwStyle, bool bMenu, WindowExStyles exStyle);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            int exStyle,
            string className,
            string windowName,
            int style,
            int x, int y,
            int width, int height,
            IntPtr hwndParent,
            IntPtr Menu,
            IntPtr Instance,
            IntPtr pvParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool DestroyWindow(IntPtr windowHandle);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags flags);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, int x, int y, int cx, int cy, SetWindowPosFlags flags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, WCmd wCmd);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpRect);

        [DllImport(LibraryName)]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport(LibraryName)]
        public static extern int TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool SetWindowTextA(IntPtr hWnd, string text);

        [DllImport(LibraryName)]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        [DllImport(LibraryName)]
        public static extern bool SetProcessDPIAware();

        [DllImport(LibraryName)]
        public static extern bool ClipCursor(ComRect lpRect);

        public static void LockCursor(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                _ = ClipCursor(null);
            }
            else
            {
                _ = GetClientRect(hwnd, out Rect rect);
                var loc = rect.Location;
                _ = ClientToScreen(hwnd, ref loc);
                rect.Location = loc;
                _ = ClipCursor(rect);
            }
        }
    }
}

#pragma warning restore CA1069 // Enumerationswerte dürfen nicht dupliziert werden