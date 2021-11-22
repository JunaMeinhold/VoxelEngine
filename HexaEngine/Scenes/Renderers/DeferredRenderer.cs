namespace HexaEngine.Scenes.Renderers
{
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Scripting;
    using HexaEngine.Shaders;
    using HexaEngine.Shaders.BuildIn.Deferred;
    using HexaEngine.Windows;
    using System.Collections.Generic;
    using System.Numerics;

    public class DeferredRenderer : IDeferredRenderer
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

        private static bool SIsInitialized { get; set; }

        public DeferredRenderer()
        {
            DeferredShader = ResourceManager.LoadShader<DeferredLightShader>();
            DeferredShader.Directional = Light is not null ? Light : new DirectionalLight()
            {
                Position = new Vector3(0, 10, -10),
                Direction = new(0, -0.5f, 0.5f),
                DiffuseColor = new Vector4(1f, 1f, 1f, 1),
                Width = 1024 * 8
            };
            DeferredShader.Directional.GenerateViewMatrix();
            DeferredShader.FogDescription = new()
            {
                FogStart = 0.8f,
                FogEnd = 10f,
            };
            SIsInitialized = true;
            DeferredShader.Directional.Initialize();
        }

        public void RenderGBuffers()
        {
            DeferredShader.GBuffers.ClearAndSetRenderTargets(DeviceManager.Current.ID3D11DeviceContext);
        }

        public void RenderLights(List<ILight> lights, List<HexaElement> elements)
        {
            DeferredShader.Directional.Render(elements);
            foreach (var light in lights)
            {
                light.Render(elements);
            }
        }

        public void RenderCompose(IView view)
        {
            DeferredShader.Render(view);
        }
    }
}