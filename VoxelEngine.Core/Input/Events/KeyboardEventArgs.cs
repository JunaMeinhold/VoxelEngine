namespace VoxelEngine.Core.Input.Events
{
    using Silk.NET.SDL;
    using VoxelEngine.Core.Input;

    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs()
        {
        }

        public KeyboardEventArgs(Key keyCode, KeyState keyState, Scancode scancode)
        {
            KeyCode = keyCode;
            KeyState = keyState;
            Scancode = scancode;
        }

        public Key KeyCode { get; internal set; }

        public KeyState KeyState { get; internal set; }

        public Scancode Scancode { get; internal set; }
    }
}