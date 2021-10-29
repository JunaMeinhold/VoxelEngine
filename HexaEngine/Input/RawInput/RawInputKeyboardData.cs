namespace HexaEngine.Input.RawInput
{
    using System.Runtime.InteropServices;
    using HexaEngine.Input.RawInput.Native;

    public class RawInputKeyboardData : RawInputData
    {
        public RawKeyboard Keyboard { get; }

        public RawInputKeyboardData(RawInputHeader header, RawKeyboard keyboard)
            : base(header) =>
            Keyboard = keyboard;

        public override unsafe byte[] ToStructure()
        {
            int headerSize = MarshalEx.SizeOf<RawInputHeader>();
            int mouseSize = MarshalEx.SizeOf<RawKeyboard>();
            byte[] bytes = new byte[headerSize + mouseSize];

            fixed (byte* bytesPtr = bytes)
            {
                *(RawInputHeader*)bytesPtr = Header;
                *(RawKeyboard*)(bytesPtr + headerSize) = Keyboard;
            }

            return bytes;
        }

        public override string ToString() =>
            $"{{{Header}, {Keyboard}}}";
    }
}