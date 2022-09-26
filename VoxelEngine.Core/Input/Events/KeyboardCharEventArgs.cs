namespace VoxelEngine.Core.Input.Events
{
    public class KeyboardCharEventArgs : EventArgs
    {
        public KeyboardCharEventArgs(char @char)
        {
            Char = @char;
        }

        public char Char;
    }
}