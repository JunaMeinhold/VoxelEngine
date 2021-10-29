namespace HexaEngine.Input.RawInput.Native
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct HidPCapsNotRange
    {
        public ushort Usage;
        private readonly ushort Reserved1;
        public ushort StringIndex;
        private readonly ushort Reserved2;
        public ushort DesignatorIndex;
        private readonly ushort Reserved3;
        public ushort DataIndex;
        private readonly ushort Reserved4;
    }
}