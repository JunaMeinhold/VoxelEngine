namespace HexaEngine.Objects.Renderers
{
    using HexaEngine.Extensions;
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Shaders.BuildIn.Texture;
    using HexaEngine.Windows;
    using System.Numerics;

    public class UIRenderer : IForwardRenderer
    {
        public Matrix4x4 viewMatrix = MatrixExtensions.LookAtLH(-Vector3.UnitZ, Vector3.UnitZ + -Vector3.UnitZ, Vector3.UnitY);
        public Matrix4x4 projectMatrix;
        public Crosshair Crosshair { get; set; }
        private TextureShader TextureShader { get; set; }
        public bool IsInitialized { get; private set; }

        public void Initialize(DeviceManager manager)
        {
            manager.OnResize += Manager_OnResize;
            Manager_OnResize(null, null);
            TextureShader = ResourceManager.LoadShader<TextureShader>();
            IsInitialized = true;
        }

        public void Render(DeviceManager manager, IView view)
        {
            if (Crosshair is not null)
            {
                Crosshair.Render(manager.ID3D11DeviceContext);
                TextureShader.Render(viewMatrix * projectMatrix, 6);
            }
        }

        public void Uninitialize()
        {
            Crosshair.Dispose();
            TextureShader.Dispose();
            IsInitialized = false;
        }

        private void Manager_OnResize(object sender, System.EventArgs e)
        {
            projectMatrix = MatrixExtensions.OrthoLH(DeviceManager.Current.Width, DeviceManager.Current.Height, 0.01f, 10000f);
        }
    }
}