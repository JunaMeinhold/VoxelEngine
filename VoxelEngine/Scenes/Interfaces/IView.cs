using HexaEngine.Scenes.Objects;
using System.Numerics;

namespace HexaEngine.Scenes.Interfaces
{
    public interface IView
    {
        public Matrix4x4 ViewMatrix { get; }

        public Matrix4x4 ProjectionMatrix { get; }

        public Vector3 Position { get; }

        public float NearPlane { get; }

        public float FarPlane { get; }

        public Frustum Frustum { get; }
    }
}