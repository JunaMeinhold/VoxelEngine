namespace HexaEngine.Scenes.Interfaces
{
    using HexaEngine.Scripting;
    using System.Collections.Generic;

    public interface IDeferredRenderer
    {
        public void RenderGBuffers();

        public void RenderLights(List<ILight> lights, List<HexaElement> elements);

        public void RenderCompose(IView view);
    }
}