namespace VoxelEngine.Core.Events
{
    using VoxelEngine.Core;

    public class HiddenEventArgs : RoutedEventArgs
    {
        public HiddenEventArgs(WindowState oldState, WindowState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public WindowState OldState { get; }

        public WindowState NewState { get; }
    }
}