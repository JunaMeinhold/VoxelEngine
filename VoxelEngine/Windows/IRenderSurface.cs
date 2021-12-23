using System;
using System.Drawing;

namespace HexaEngine.Windows
{
    public interface IRenderSurface
    {
        /// <summary>
        /// Window HWND handle
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// The Surface Offset on x-axis
        /// </summary>
        public int ViewOffsetX { get; }

        /// <summary>
        /// The Surface Offset on y-axis
        /// </summary>
        public int ViewOffsetY { get; }

        /// <summary>
        /// The Surface Width
        /// </summary>
        public int ViewWidth { get; }

        /// <summary>
        /// The Surface Height
        /// </summary>
        public int ViewHeight { get; }

        /// <summary>
        /// The Width of the parent Window
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The Height of the parent Window
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Enables/Disables exclusive fullscreen
        /// </summary>
        public bool Fullscreen { get; }

        public Color BackgroundClear { get; set; }
    }
}