namespace HexaEngine.Input.RawInput.Native
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// RAWKEYBOARD
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawKeyboard
    {
        private readonly ushort usMakeCode;
        private readonly RawKeyboardFlags usFlags;
        private readonly ushort usReserverd;
        private readonly ushort usVKey;
        private readonly uint ulMessage;
        private readonly uint ulExtraInformation;

        public int ScanCode => usMakeCode;
        public RawKeyboardFlags Flags => usFlags;
        public int VirutalKey => usVKey;
        public uint WindowMessage => ulMessage;
        public uint ExtraInformation => ulExtraInformation;

        public override string ToString() =>
            $"{{Key: {VirutalKey}, ScanCode: {ScanCode}, Flags: {Flags}}}";
    }

    [Flags]
    public enum RawKeyboardFlags : ushort
    {
        Down,
        Up,
        LeftKey,
        RightKey = 4,
    }
}