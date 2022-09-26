namespace App.Objects
{
    using App.Renderers.Forward;
    using VoxelEngine.Scenes;

    public class Skybox : SceneElement
    {
        private readonly SkyboxRenderer component;

        public Skybox()
        {
            component = new SkyboxRenderer();
            component.TexturePath = "skybox.dds";
            AddComponent(component);
        }
    }
}