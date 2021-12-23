namespace HexaEngine.Objects.Renderers
{
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Shaders;
    using HexaEngine.Shaders.BuildIn.Deferred;
    using HexaEngine.Windows;
    using System.Numerics;

    public class DeferredRenderer : IRenderer
    {
        private static DirectionalLight light;

        public static DeferredLightShader DeferredShader { get; private set; }

        public static DirectionalLight Light
        {
            get => light;
            set
            {
                if (SIsInitialized)
                    DeferredShader.Directional = value;
                light = value;
            }
        }

        public bool IsInitialized { get => SIsInitialized; }

        private static bool SIsInitialized { get; set; }

        public void BeginRender(DeviceManager manager)
        {
            DeferredShader.GBuffers.ClearAndSetRenderTargets(manager.ID3D11DeviceContext);
        }

        public void EndRender(DeviceManager manager, IView view)
        {
            DeferredShader.Render();
        }

        public void Initialize(DeviceManager manager)
        {
            SInitialize(manager);
        }

        public void Uninitialize()
        {
            SUninitialize();
        }

        public static void SInitialize(DeviceManager manager)
        {
            DeferredShader = ResourceManager.LoadShader<DeferredLightShader>();
            DeferredShader.Directional = Light is not null ? Light : new DirectionalLight()
            {
                Position = new Vector3(0, 2, 0),
                Direction = new Vector3(0, -0.5f, 0.5f),
                DiffuseColor = new Vector4(1f, 1f, 1f, 1),
                Width = 1024
            };
            DeferredShader.FogDescription = new()
            {
                FogStart = 0.8f,
                FogEnd = 10f,
            };
            SIsInitialized = true;
        }

        public static void SUninitialize()
        {
            DeferredShader.Dispose();
            SIsInitialized = false;
        }
    }
}