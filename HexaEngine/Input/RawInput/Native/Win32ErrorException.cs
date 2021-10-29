namespace HexaEngine.Input.RawInput.Native
{
    using System;
    using System.Runtime.InteropServices;

    public class Win32ErrorException : Exception
    {
        public Win32ErrorException()
            : this(Marshal.GetLastWin32Error())
        {
        }

        public Win32ErrorException(int win32ErrorCode)
            : base(Kernel32.FormatMessage(win32ErrorCode))
        {
        }
    }
}