namespace HexaEngine.Input.RawInput.Events
{
    using HexaEngine.Input;
    using System;

    public class BaseRawInputEventArgs : EventArgs
    {
        public bool IsDown { get; internal set; }

        public Keys Key { get; internal set; }
    }
}