using HexaEngine.Resources;
using HexaEngine.Windows;
using System.Numerics;

namespace HexaEngine.Scenes.Interfaces
{
    public interface IObjectRenderer
    {
        public void Render(DeviceManager manager, IView view, ISceneObject sceneObject, Matrix4x4 transform);

        public void RenderInstanced(DeviceManager manager, IView view, ISceneObject sceneObject, Matrix4x4 transform, InstanceType[] instances);

        public void Initialize(DeviceManager manager);

        public void Uninitialize();

        public bool IsInitialized { get; }
    }
}