namespace HexaEngine
{
    using HexaEngine.Scenes;
    using HexaEngine.Windows;

    public abstract class ScenePrefab
    {
        public Scene Scene { get; set; }

        internal Scene InternalCreateInstance(RenderWindow window)
        {
            Scene = new(window, DeviceManager.Current);
            CreateInstance();
            return Scene;
        }

        public abstract void CreateInstance();
    }
}