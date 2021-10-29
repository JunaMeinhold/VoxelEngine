using HexaEngine.Input;
using HexaEngine.Input.Events;
using HexaEngine.Input.RawInput.Native;
using System;

namespace HexaEngine.Input.RawInput.Events
{
    public class MouseRawInputEventArgs : EventArgs
    {
        public MouseRawInputEventArgs(RawInputMouseData data)
        {
            X = data.Mouse.LastX;
            Y = data.Mouse.LastY;
            Flags = data.Mouse.Flags;
            Data = data;
            if (Data.Mouse.Buttons != RawMouseButtonFlags.None)
                switch (Data.Mouse.Buttons)
                {
                    case RawMouseButtonFlags.LeftButtonDown:
                        Key = MouseButton.LButton;
                        IsDown = true;
                        break;

                    case RawMouseButtonFlags.LeftButtonUp:
                        Key = MouseButton.LButton;
                        IsDown = false;
                        break;

                    case RawMouseButtonFlags.RightButtonDown:
                        Key = MouseButton.RButton;
                        IsDown = true;
                        break;

                    case RawMouseButtonFlags.RightButtonUp:
                        Key = MouseButton.RButton;
                        IsDown = false;
                        break;

                    case RawMouseButtonFlags.MiddleButtonDown:
                        Key = MouseButton.MButton;
                        IsDown = true;
                        break;

                    case RawMouseButtonFlags.MiddleButtonUp:
                        Key = MouseButton.MButton;
                        IsDown = false;
                        break;
                }
        }

        public MouseRawInputEventArgs(MouseButton keys, bool state, int x, int y)
        {
            Data = null;
            IsDown = state;
            Key = keys;
            X = x;
            Y = y;
        }

        public RawInputMouseData Data { get; }

        public int X { get; }

        public int Y { get; }

        public RawMouseFlags Flags { get; }

        public bool IsDown { get; internal set; }

        public MouseButton Key { get; internal set; }

        public static implicit operator MouseEventArgs(MouseRawInputEventArgs args)
        {
            return new MouseEventArgs(args.Key, args.IsDown ? MouseButtonState.Pressed : MouseButtonState.Released, default);
        }
    }
}