namespace HexaEngine.Input.RawInput.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class Kernel32
    {
        [DllImport("kernel32", EntryPoint = "CreateFile", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileCore(string lpFileName, DesiredAccess dwDesiredAccess, ShareMode dwShareMode, IntPtr lpSecurityAttributes, CreateDisposition dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32", EntryPoint = "GetModuleHandle", SetLastError = true)]
        private static extern IntPtr GetModuleHandleCore(string lpModuleName);

        [DllImport("kernel32", EntryPoint = "GetProcAddress", SetLastError = true)]
        private static extern IntPtr GetProcAddressCore(IntPtr hModule, string procName);

        [DllImport("kernel32", EntryPoint = "IsWow64Process", SetLastError = true)]
        private static extern bool IsWow64ProcessCore(IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("kernel32")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32", SetLastError = true)]
        private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr Arguments);

        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        [Flags]
        public enum DesiredAccess : uint
        {
            None,
            Write = 0x40000000,
            Read = 0x80000000
        }

        [Flags]
        public enum ShareMode : uint
        {
            None,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004
        }

        public enum CreateDisposition : uint
        {
            CreateNew = 1,
            CreateAlways,
            OpenExisting,
            OpenAlways,
            TruncateExisting
        }

        public static IntPtr GetModuleHandle(string moduleName)
        {
            IntPtr hModule = GetModuleHandleCore(moduleName);
            if (hModule == IntPtr.Zero) throw new Win32ErrorException();

            return hModule;
        }

        public static IntPtr GetProcAddress(IntPtr hModule, string procName)
        {
            IntPtr farProc = GetProcAddressCore(hModule, procName);
            if (farProc == IntPtr.Zero) throw new Win32ErrorException();

            return farProc;
        }

        public static bool IsWow64Process(IntPtr hProcess)
        {
            if (!IsWow64ProcessCore(hProcess, out bool result)) throw new Win32ErrorException();

            return result;
        }

        public static IntPtr CreateFile(
            string fileName,
            ShareMode shareMode,
            CreateDisposition creationDisposition,
            DesiredAccess desiredAccess = DesiredAccess.None,
            IntPtr securityAttributes = default,
            uint flagsAndAttributes = 0,
            IntPtr templateFile = default)
        {
            IntPtr handle = CreateFileCore(fileName, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);
            if (handle == new IntPtr(-1)) throw new Win32ErrorException();

            return handle;
        }

        public static bool TryCreateFile(
            string fileName,
            ShareMode shareMode,
            CreateDisposition creationDisposition,
            out IntPtr handle,
            DesiredAccess desiredAccess = DesiredAccess.None,
            IntPtr securityAttributes = default,
            uint flagsAndAttributes = 0,
            IntPtr templateFile = default)
        {
            handle = CreateFileCore(fileName, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile);

            return handle != new IntPtr(-1);
        }

        public static string FormatMessage(int errorCode)
        {
            StringBuilder message = new StringBuilder(255);
            uint charsWritten = FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, (uint)errorCode, 0, message, message.Capacity, IntPtr.Zero);
            if (charsWritten == 0) throw new Win32ErrorException();

            return message.ToString();
        }
    }
}