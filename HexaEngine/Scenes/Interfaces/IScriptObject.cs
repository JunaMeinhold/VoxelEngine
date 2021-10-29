namespace HexaEngine.Scenes.Interfaces
{
    public interface IScriptObject : ISceneObject
    {
        public void Initialize();

        public void Uninitialize();

        public void Awake();

        public void Sleep();

        public void UpdateFixed();
    }

    public interface IFrameScriptObject : IScriptObject
    {
        public void Update();
    }
}