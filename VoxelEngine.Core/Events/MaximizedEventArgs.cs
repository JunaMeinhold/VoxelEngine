namespace VoxelEngine.Core.Events
{
    using VoxelEngine.Core;

    public class MaximizedEventArgs : RoutedEventArgs
    {
        public MaximizedEventArgs(WindowState oldState, WindowState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public WindowState OldState { get; }

        public WindowState NewState { get; }
    }
}