namespace VoxelEngine.Core.Input.Events
{
    using Silk.NET.SDL;
    using VoxelEngine.Core.Input;

    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(KeyCode keyCode, KeyState keyState)
        {
            KeyCode = keyCode;
            KeyState = keyState;
        }

        public KeyCode KeyCode { get; }

        public KeyState KeyState { get; }
    }
}