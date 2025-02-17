﻿namespace VoxelEngine.Core.Input
{
    using System.Numerics;
    using System.Text;
    using Hexa.NET.SDL2;
    using VoxelEngine.Core.Input.Events;

    public unsafe class Joystick : IDisposable
    {
        private readonly int id;
        internal readonly SDLJoystick* joystick;
        private readonly Dictionary<int, short> axes = new();
        private readonly Dictionary<int, (int, int)> balls = new();
        private readonly Dictionary<int, JoystickButtonState> buttons = new();
        private readonly Dictionary<int, JoystickHatState> hats = new();

        private readonly JoystickAxisMotionEventArgs axisMotionEventArgs = new();
        private readonly JoystickBallMotionEventArgs ballMotionEventArgs = new();
        private readonly JoystickButtonEventArgs buttonEventArgs = new();
        private readonly JoystickHatMotionEventArgs hatMotionEventArgs = new();

        private readonly string guid;
        private short deadzone = 8000;

        private bool disposedValue;

        public Joystick(int id)
        {
            this.id = id;
            joystick = SDL.JoystickOpen(id);
            if (joystick == null)
                SdlCheckError();

            var axisCount = SDL.JoystickNumAxes(joystick);
            for (int i = 0; i < axisCount; i++)
            {
                short state;
                SDL.JoystickGetAxisInitialState(joystick, i, &state);
                axes.Add(i, state);
            }

            var ballCount = SDL.JoystickNumBalls(joystick);
            for (int i = 0; i < ballCount; i++)
            {
                int x;
                int y;
                SDL.JoystickGetBall(joystick, i, &x, &y);
                balls.Add(i, new(x, y));
            }

            var buttonCount = SDL.JoystickNumButtons(joystick);
            for (int i = 0; i < buttonCount; i++)
            {
                var state = (JoystickButtonState)SDL.JoystickGetButton(joystick, i);
                buttons.Add(i, state);
            }

            var hatCount = SDL.JoystickNumHats(joystick);
            for (int i = 0; i < hatCount; i++)
            {
                var state = (JoystickHatState)SDL.JoystickGetHat(joystick, i);
                hats.Add(i, state);
            }

            var guid = SDL.JoystickGetGUID(joystick);
            SdlCheckError();
            var buffer = AllocT<byte>(33);
            SDL.JoystickGetGUIDString(guid, buffer, 33);
            var size = StrLen(buffer);
            var value = Encoding.ASCII.GetString(buffer, size - 1);
            Free(buffer);
            this.guid = value;
        }

        public int Id => id;

        public string Name
        {
            get
            {
                var name = SDL.JoystickNameS(joystick);
                if (name == null)
                    SdlCheckError();
                return name;
            }
        }

        public ushort Vendor => SDL.JoystickGetVendor(joystick);

        public ushort Product => SDL.JoystickGetProduct(joystick);

        public ushort ProductVersion => SDL.JoystickGetProductVersion(joystick);

        public string Serial => SDL.JoystickGetSerialS(joystick);

        public string Guid => guid;

        public bool IsAttached => SDL.JoystickGetAttached(joystick) == SDLBool.True;

        public bool IsVirtual => SDL.JoystickIsVirtual(id) == SDLBool.True;

        public bool HasLED => SDL.JoystickHasLED(joystick) == SDLBool.True;

        public JoystickType Type => Helper.Convert(SDL.JoystickGetType(joystick));

        public short Deadzone { get => deadzone; set => deadzone = value; }

        public int PlayerIndex { get => SDL.JoystickGetPlayerIndex(joystick); set => SDL.JoystickSetPlayerIndex(joystick, value); }

        public JoystickPowerLevel PowerLevel => Helper.Convert(SDL.JoystickCurrentPowerLevel(joystick));

        public IReadOnlyDictionary<int, short> Axes => axes;

        public IReadOnlyDictionary<int, (int, int)> Balls => balls;

        public IReadOnlyDictionary<int, JoystickButtonState> Buttons => buttons;

        public IReadOnlyDictionary<int, JoystickHatState> Hats => hats;

        public event EventHandler<JoystickAxisMotionEventArgs>? AxisMotion;

        public event EventHandler<JoystickBallMotionEventArgs>? BallMotion;

        public event EventHandler<JoystickButtonEventArgs>? ButtonDown;

        public event EventHandler<JoystickButtonEventArgs>? ButtonUp;

        public event EventHandler<JoystickHatMotionEventArgs>? HatMotion;

        public void Rumble(ushort lowFreq, ushort highFreq, uint durationMs)
        {
            SDL.JoystickRumble(joystick, lowFreq, highFreq, durationMs);
        }

        public void RumbleTriggers(ushort left, ushort right, uint durationMs)
        {
            SDL.JoystickRumbleTriggers(joystick, left, right, durationMs);
        }

        public void SetLED(Vector4 color)
        {
            SDL.JoystickSetLED(joystick, (byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
        }

        public void SetLED(byte red, byte green, byte blue)
        {
            SDL.JoystickSetLED(joystick, red, green, blue);
        }

        internal void OnAxisMotion(SDLJoyAxisEvent even)
        {
            if (Math.Abs((int)even.Value) < deadzone)
            {
                even.Value = 0;
            }

            if (even.Value == axes[even.Axis])
            {
                return;
            }

            axes[even.Axis] = even.Value;
            axisMotionEventArgs.Axis = even.Axis;
            axisMotionEventArgs.Value = even.Value;
            AxisMotion?.Invoke(this, axisMotionEventArgs);
        }

        internal void OnBallMotion(SDLJoyBallEvent even)
        {
            balls[even.Ball] = (even.Xrel, even.Yrel);
            ballMotionEventArgs.Ball = even.Ball;
            ballMotionEventArgs.RelX = even.Xrel;
            ballMotionEventArgs.RelY = even.Yrel;
            BallMotion?.Invoke(this, ballMotionEventArgs);
        }

        internal void OnButtonDown(SDLJoyButtonEvent even)
        {
            buttons[even.Button] = JoystickButtonState.Down;
            buttonEventArgs.Button = even.Button;
            buttonEventArgs.State = JoystickButtonState.Down;
            ButtonDown?.Invoke(this, buttonEventArgs);
        }

        internal void OnButtonUp(SDLJoyButtonEvent even)
        {
            buttons[even.Button] = JoystickButtonState.Up;
            buttonEventArgs.Button = even.Button;
            buttonEventArgs.State = JoystickButtonState.Up;
            ButtonUp?.Invoke(this, buttonEventArgs);
        }

        internal void OnHatMotion(SDLJoyHatEvent even)
        {
            hats[even.Hat] = (JoystickHatState)even.Value;
            hatMotionEventArgs.Hat = even.Hat;
            hatMotionEventArgs.State = (JoystickHatState)even.Value;
            HatMotion?.Invoke(this, hatMotionEventArgs);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                SDL.JoystickClose(joystick);
                disposedValue = true;
            }
        }

        ~Joystick()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}