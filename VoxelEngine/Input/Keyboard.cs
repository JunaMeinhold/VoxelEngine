using HexaEngine.Input.Events;
using HexaEngine.Input.RawInput;
using HexaEngine.Input.RawInput.Events;
using HexaEngine.Windows.Native;
using System;
using System.Collections.Generic;

namespace HexaEngine.Input
{
    public static class Keyboard
    {
        public static void Initialize()
        {
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                var state = (KeyStates)User32.GetKeyState((int)key);
                KeyStates[key] = state;
            }
        }

        public static Dictionary<Keys, KeyStates> KeyStates { get; } = new();

        public static event EventHandler<KeyboardEventArgs> OnKeyDown;

        public static event EventHandler<KeyboardEventArgs> OnKeyUp;

        public static KeyboardEventArgs Update(Keys key, KeyStates state)
        {
            KeyStates[key] = state;
            var args = new KeyboardEventArgs(key, state);
            if (state == Input.KeyStates.Pressed | state == Input.KeyStates.Toggled)
            {
                OnKeyDown?.Invoke(null, args);
            }
            else if (state == Input.KeyStates.Released)
            {
                OnKeyUp?.Invoke(null, args);
            }
            return args;
        }

        public static KeyboardRawInputEventArgs Update(RawInputKeyboardData data)
        {
            var args = new KeyboardRawInputEventArgs(data);
            var state = KeyStates[args.Key] = args.IsDown ? Input.KeyStates.Pressed : Input.KeyStates.Released;
            if (state == Input.KeyStates.Pressed | state == Input.KeyStates.Toggled)
            {
                OnKeyDown?.Invoke(null, args);
            }
            else if (state == Input.KeyStates.Released)
            {
                OnKeyUp?.Invoke(null, args);
            }
            return args;
        }

        public static bool IsDown(Keys n)
        {
            return KeyStates[n] == Input.KeyStates.Pressed;
        }
    }
}