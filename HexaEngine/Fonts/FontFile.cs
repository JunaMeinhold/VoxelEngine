using System;
using System.Runtime.InteropServices;

namespace HexaEngine.Fonts
{
    public class FontFile
    {
        [DllImport("gdi.dll")]
        public static extern int AddFontResourceExA(string name, uint fl, IntPtr pdv);

        public int Count { get; set; }

        public void FromFile()
        {
        }
    }
}