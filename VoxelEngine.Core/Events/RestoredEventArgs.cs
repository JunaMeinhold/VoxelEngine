namespace VoxelEngine.Core.Events
{
    using VoxelEngine.Core;

    public class RestoredEventArgs : RoutedEventArgs
    {
        public RestoredEventArgs(WindowState oldState, WindowState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public WindowState OldState { get; }

        public WindowState NewState { get; }
    }
}