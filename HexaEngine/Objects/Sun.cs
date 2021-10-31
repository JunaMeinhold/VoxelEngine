namespace HexaEngine.Objects
{
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Shaders;
    using System;
    using System.Numerics;

    public class Sun : DirectionalLight
    {
        public Sun()
        {
            AmbientColor = new Vector4(0.15f, 0.15f, 0.15f, 0.15f);
            Width = 1024;
        }

        private const float DegToRadFactor = 0.0174532925f;

        public float Distance { get; } = 1000f;

        public float Angle { get; private set; }

        public void Update(IView view, int time)
        {
            Angle = time / 24000f * 360;
            Direction = new Vector3(MathF.Cos(Angle * DegToRadFactor), MathF.Sin(Angle * DegToRadFactor), 0.5f);
            float x = (float)(MathF.Cos(Angle * DegToRadFactor) * Distance);
            float y = (float)(MathF.Sin(Angle * DegToRadFactor) * Distance);
            Position = new Vector3(x + view.Position.X, y + view.Position.Y, view.Position.Z);
            GenerateViewMatrix();
        }
    }
}