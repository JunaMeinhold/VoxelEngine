namespace HexaEngine.Input.RawInput.Native
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// RAWINPUTHEADER
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputHeader
    {
        private readonly RawInputDeviceType dwType;
        private readonly int dwSize;
        private readonly RawInputDeviceHandle hDevice;
        private readonly IntPtr wParam;

        public RawInputDeviceType Type => dwType;
        public int Size => dwSize;
        public RawInputDeviceHandle DeviceHandle => hDevice;
        public IntPtr WParam => wParam;

        public override string ToString() =>
            $"{{{Type}: {DeviceHandle}, WParam: {WParam}}}";
    }
}