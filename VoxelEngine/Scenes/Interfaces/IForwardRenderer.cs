namespace HexaEngine.Scenes.Interfaces
{
    using HexaEngine.Windows;

    public interface IForwardRenderer
    {
        public void Render(DeviceManager manager, IView view);

        public void Initialize(DeviceManager manager);

        public void Uninitialize();

        public bool IsInitialized { get; }
    }
}