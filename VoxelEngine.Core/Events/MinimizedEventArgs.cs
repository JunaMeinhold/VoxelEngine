namespace VoxelEngine.Core.Events
{
    using VoxelEngine.Core;

    public class MinimizedEventArgs : RoutedEventArgs
    {
        public MinimizedEventArgs(WindowState oldState, WindowState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public WindowState OldState { get; }

        public WindowState NewState { get; }
    }
}