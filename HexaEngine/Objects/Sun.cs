namespace HexaEngine.Objects
{
    using HexaEngine.Shaders;
    using System;
    using System.Numerics;

    public class Sun : DirectionalLight
    {
        private const float DegToRadFactor = 0.0174532925f;
        public float Distance { get; } = 1000f;

        public float Angle { get; }

        public void UpdatePosition(Vector3 center)
        {
            float x = (float)(Math.Cos(Angle * DegToRadFactor) * Distance);
            float y = (float)(Math.Sin(Angle * DegToRadFactor) * Distance);
        }
    }
}