using System;
using System.Drawing;

namespace HexaEngine.Input.Events
{
    public class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(MouseButton button, MouseButtonState state, PointF position)
        {
            Button = button;
            State = state;
            Position = position;
        }

        public MouseButton Button { get; }

        public MouseButtonState State { get; }

        public PointF Position { get; set; }

        public bool Handled { get; set; }
    }
}