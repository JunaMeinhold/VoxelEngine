using HexaEngine.Windows;

namespace HexaEngine.Scenes.Interfaces
{
    public interface IRenderer
    {
        public void EndRender(DeviceManager manager, IView view);

        public void BeginRender(DeviceManager manager);

        public void Initialize(DeviceManager manager);

        public void Uninitialize();

        public bool IsInitialized { get; }
    }
}