using HexaEngine.Mathematics;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Scenes.Objects;
using System;
using System.Numerics;

namespace HexaEngine.Shaders
{
    public class DirectionalLight : IView
    {
        private const float DegToRadFactor = 0.0174532925f;
        private Vector3 position;
        private Vector3 lookAt;
        public Vector4 AmbientColor { get; set; }
        public Vector4 DiffuseColor { get; set; }
        public Vector3 Position { get => position; set { position = value; GenerateViewMatrix(); } }
        public Vector3 Direction { get => lookAt; set { lookAt = value; GenerateViewMatrix(); } }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }
        public float RotationY { get; set; }
        public int Width { get; set; } = 1024;
        public float NearPlane { get; } = 0.1f;
        public float FarPlane { get; } = 1000f;
        public Frustum Frustum { get; set; }

        public void GenerateViewMatrix()
        {
            GenerateProjectionMatrix();

            // Calculate the rotation in radians.
            var radians = RotationY * DegToRadFactor;

            // Setup where the camera is looking.
            Vector3 lookAt = new();
            lookAt.X = Position.X;
            lookAt.Y = Position.Y;
            lookAt.Z = MathF.Cos(radians) + Position.Z;
            // Create the view matrix from the three vectors.
            ViewMatrix = Mathematics.Extensions.LookAtLH(position, Direction + position, Vector3.UnitY);
            Frustum = new Frustum(FarPlane, ProjectionMatrix, ViewMatrix);
        }

        public void GenerateProjectionMatrix()
        {
            // Create the projection matrix for the light.
            ProjectionMatrix = Mathematics.Extensions.OrthoLH(Width, Width, NearPlane, FarPlane);
        }
    }
}