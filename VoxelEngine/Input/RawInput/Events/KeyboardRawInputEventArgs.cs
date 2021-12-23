using HexaEngine.Input;
using HexaEngine.Input.Events;
using HexaEngine.Input.RawInput.Native;

namespace HexaEngine.Input.RawInput.Events
{
    public class KeyboardRawInputEventArgs : BaseRawInputEventArgs
    {
        public KeyboardRawInputEventArgs(RawInputKeyboardData data)
        {
            Data = data;
            IsDown = Data.Keyboard.Flags == RawKeyboardFlags.Down;
            Key = (Keys)Data.Keyboard.VirutalKey;
        }

        public KeyboardRawInputEventArgs(Keys keys, bool state)
        {
            Data = null;
            IsDown = state;
            Key = keys;
        }

        public static implicit operator KeyboardEventArgs(KeyboardRawInputEventArgs args)
        {
            return new KeyboardEventArgs(args.Key, args.IsDown ? KeyStates.Pressed : KeyStates.Released);
        }

        public RawInputKeyboardData Data { get; }
    }
}