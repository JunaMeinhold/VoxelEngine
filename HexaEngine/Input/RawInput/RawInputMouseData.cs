namespace HexaEngine.Input.RawInput
{
    using System.Runtime.InteropServices;
    using HexaEngine.Input.RawInput.Native;

    public class RawInputMouseData : RawInputData
    {
        public RawMouse Mouse { get; }

        public RawInputMouseData(RawInputHeader header, RawMouse mouse)
            : base(header) =>
            Mouse = mouse;

        public override unsafe byte[] ToStructure()
        {
            int headerSize = MarshalEx.SizeOf<RawInputHeader>();
            int mouseSize = MarshalEx.SizeOf<RawMouse>();
            byte[] bytes = new byte[headerSize + mouseSize];

            fixed (byte* bytesPtr = bytes)
            {
                *(RawInputHeader*)bytesPtr = Header;
                *(RawMouse*)(bytesPtr + headerSize) = Mouse;
            }

            return bytes;
        }

        public override string ToString() =>
            $"{{{Header}, {Mouse}}}";
    }
}