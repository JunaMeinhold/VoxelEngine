using System;

namespace HexaEngine.Input.Events
{
    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(Keys keys, KeyStates state)
        {
            Key = keys;
            State = state;
        }

        public Keys Key { get; }
        public KeyStates State { get; }

        public bool Handled { get; set; }
    }
}