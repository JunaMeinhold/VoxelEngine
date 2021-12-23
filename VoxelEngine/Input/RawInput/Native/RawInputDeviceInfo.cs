namespace HexaEngine.Input.RawInput.Native
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// RID_DEVICE_INFO
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RawInputDeviceInfo
    {
        [FieldOffset(0)]
        private readonly int sbSize;

        [FieldOffset(4)]
        private readonly RawInputDeviceType dwType;

        [FieldOffset(8)]
        private readonly RawInputMouseInfo mouse;

        [FieldOffset(8)]
        private readonly RawInputKeyboardInfo keyboard;

        [FieldOffset(8)]
        private readonly RawInputHidInfo hid;

        /// <summary>
        /// dwType
        /// </summary>
        public RawInputDeviceType Type => dwType;

        /// <summary>
        /// mouse
        /// </summary>
        public RawInputMouseInfo Mouse => mouse;

        /// <summary>
        /// keyboard
        /// </summary>
        public RawInputKeyboardInfo Keyboard => keyboard;

        /// <summary>
        /// hid
        /// </summary>
        public RawInputHidInfo Hid => hid;
    }
}