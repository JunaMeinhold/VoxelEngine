using HexaEngine.Windows.Native;
using System.Drawing;

namespace HexaEngine.Windows
{
    public class Screen
    {
        public Screen()
        {
            Width = User32.GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
            Height = User32.GetSystemMetrics(SystemMetrics.SM_CYSCREEN);
            Size = new Size(Width, Height);
        }

        public int Width { get; }

        public int Height { get; }

        public Size Size { get; }

        public static Screen PrimaryScreen { get; } = new();
    }
}