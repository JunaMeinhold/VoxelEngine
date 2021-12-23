using HexaEngine.Input.Events;
using HexaEngine.Input.RawInput;
using HexaEngine.Input.RawInput.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace HexaEngine.Input
{
    public static class Mouse
    {
        private static readonly Dictionary<MouseButton, MouseButtonState> buttons = new();

        public static void Initialize()
        {
            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
            {
                buttons.Add(button, MouseButtonState.Released);
            }
        }

        public static bool IsDown(MouseButton button)
        {
            return buttons[button] == MouseButtonState.Pressed;
        }

        public static IReadOnlyDictionary<MouseButton, MouseButtonState> Buttons => buttons;

        public static Vector2 PositionVector { get; private set; }

        public static Point Position { get; private set; }

        public static bool Hover { get; private set; }

        private static Vector2 Delta { get; set; }

        internal static MouseEventArgs Update(MouseButton button, MouseButtonState state)
        {
            buttons[button] = state;
            return new MouseEventArgs(button, state, Position);
        }

        internal static MouseEventArgs Update(Point point)
        {
            Position = point;
            PositionVector = new Vector2(point.X, point.Y);
            return new MouseEventArgs(MouseButton.None, MouseButtonState.Released, Position);
        }

        internal static MouseRawInputEventArgs Update(RawInputMouseData data)
        {
            var args = new MouseRawInputEventArgs(data);
            buttons[args.Key] = args.IsDown ? MouseButtonState.Pressed : MouseButtonState.Released;
            Delta += new Vector2(args.X, args.Y);
            return args;
        }

        internal static MouseEventArgs Update(bool state)
        {
            Hover = state;
            return new MouseEventArgs(MouseButton.None, MouseButtonState.Released, Position);
        }

        public static Vector2 GetDelta()
        {
            var temp = Delta;
            Delta = Vector2.Zero;
            return temp;
        }
    }
}