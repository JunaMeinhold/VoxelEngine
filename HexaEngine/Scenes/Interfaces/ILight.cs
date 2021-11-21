namespace HexaEngine.Scenes.Interfaces
{
    using HexaEngine.Resources;
    using HexaEngine.Scripting;
    using System.Collections.Generic;

    public interface ILight
    {
        public RenderTexture ShadowMap { get; }

        public void Initialize();

        public void Uninitialize();

        public void Render(List<HexaElement> elements);
    }
}