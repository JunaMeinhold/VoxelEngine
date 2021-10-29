using System.Numerics;

namespace HexaEngine.Scenes.Interfaces
{
    public interface ISceneObject
    {
        public IObjectRenderer Renderer { get; }

        public Matrix4x4 Transform { get; }
    }
}