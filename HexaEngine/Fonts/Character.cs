using System.Runtime.InteropServices;

namespace HexaEngine.Fonts
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Character
    {
        public float Left, Right;

        public int Size;
    }
}