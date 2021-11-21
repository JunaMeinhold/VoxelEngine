namespace HexaEngine.Scripting
{
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Shaders;
    using HexaEngine.Windows;
    using System;

    public class RendererComponent : IComponent
    {
        private HexaElement element;
        private Model model;
        private Texture texture;
        private Shader shader;

        public string Model { get; set; }

        public string Texture { get; set; }

        public Type Shader { get; set; }

        public bool CastShadows { get; set; }

        public virtual void Initialize(HexaElement element)
        {
            this.element = element;
            model = ResourceManager.LoadModelObj(Model);
            texture = ResourceManager.LoadTexture(Texture);
            shader = ResourceManager.LoadShader(Shader);
        }

        public virtual void Uninitialize()
        {
            model.Dispose();
            texture.Dispose();
            shader.Dispose();
        }

        public virtual void Update()
        {
            model.Render(DeviceManager.Current.ID3D11DeviceContext);
            texture.Render(DeviceManager.Current.ID3D11DeviceContext);
            shader.Render(element.Scene.Camera, element.Transform, model.Indices.Length);
        }

        public virtual void Render(Shader shader, IView view)
        {
            model.Render(DeviceManager.Current.ID3D11DeviceContext);
            texture.Render(DeviceManager.Current.ID3D11DeviceContext);
            shader.Render(view, element.Transform, model.Indices.Length);
        }
    }
}