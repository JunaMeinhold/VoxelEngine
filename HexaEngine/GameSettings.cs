namespace HexaEngine
{
    using HexaEngine.Input.RawInput;
    using HexaEngine.Windows;
    using System.Collections.Generic;
    using System.Drawing;

    public class GameSettings
    {
        public string Title { get; set; }

        public int Width { get; set; } = 1280;

        public int Height { get; set; } = 720;

        public bool VSync { get; set; } = true;

        public bool Fullscreen { get; set; } = false;

        public StartupLocation StartupLocation { get; set; }

        public List<HidUsageAndPage> InputTypes { get; } = new();

        public bool FPSLimit { get; set; } = false;

        public int FPSTarget { get; set; } = 60;

        public Color BackgroundClear { get; set; } = Color.Black;

        public bool CursorCapture { get; set; } = true;

        public bool CursorVisible { get; set; } = false;
    }
}