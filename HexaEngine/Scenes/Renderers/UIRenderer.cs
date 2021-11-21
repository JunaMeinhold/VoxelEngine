namespace HexaEngine.Scenes.Renderers
{
    using HexaEngine.Fonts;
    using HexaEngine.Mathematics;
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Shaders.BuildIn.Texture;
    using HexaEngine.Windows;
    using System.Collections.Generic;
    using System.Numerics;

    public class UIRenderer : IForwardRenderer
    {
        public Matrix4x4 viewMatrix = Extensions.LookAtLH(-Vector3.UnitZ, Vector3.UnitZ + -Vector3.UnitZ, Vector3.UnitY);
        public Matrix4x4 projectMatrix;

        private TextureShader TextureShader { get; set; }
        private FontShader FontShader { get; set; }
        public List<Text> Texts { get; } = new();
        public bool IsInitialized { get; private set; }

        public void Initialize(DeviceManager manager)
        {
            manager.OnResize += Manager_OnResize;
            Manager_OnResize(null, null);
            TextureShader = ResourceManager.LoadShader<TextureShader>();
            FontShader = ResourceManager.LoadShader<FontShader>();
            IsInitialized = true;
        }

        public void Render(DeviceManager manager, IView view)
        {
            foreach (Text text in Texts)
            {
                FontShader.Render(text.Transform * viewMatrix * projectMatrix, text);
            }
        }

        public void Uninitialize()
        {
            TextureShader.Dispose();
            FontShader.Dispose();
            foreach (var text in Texts)
            {
                text.Font.Dispose();
                text.Dispose();
            }
            IsInitialized = false;
        }

        private void Manager_OnResize(object sender, System.EventArgs e)
        {
            projectMatrix = Extensions.OrthoLH(DeviceManager.Current.Width, DeviceManager.Current.Height, 0.01f, 10000f);
        }
    }
}