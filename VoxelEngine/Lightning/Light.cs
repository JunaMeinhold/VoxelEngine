namespace VoxelEngine.Lightning
{
    using System.Numerics;
    using VoxelEngine.Scenes;

    public abstract class Light : GameObject
    {
        protected const float DegToRadFactor = 0.0174532925f;
        protected Vector4 color = Vector4.One;

        public Vector4 Color { get => color; set => color = value; }

        public bool CastShadows { get; set; }

        public abstract LightType Type { get; }
    }
}