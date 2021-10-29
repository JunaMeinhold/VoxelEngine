namespace HexaEngine.Objects.Renderers
{
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Scenes.Objects;
    using HexaEngine.Shaders.BuildIn.Skyboxes;
    using HexaEngine.Windows;

    public class SkyboxRenderer : IForwardRenderer
    {
        private SkyboxShader SkyboxShader { get; set; }
        public Skybox Skybox { get; set; }
        public bool IsInitialized { get; private set; }

        public void Initialize(DeviceManager manager)
        {
            SkyboxShader = ResourceManager.LoadShader<SkyboxShader>();
            IsInitialized = true;
        }

        public void Render(DeviceManager manager, IView view)
        {
            SkyboxShader.Render(view, Skybox);
        }

        public void Uninitialize()
        {
            SkyboxShader.Dispose();
            Skybox?.Dispose();
            IsInitialized = false;
        }
    }
}