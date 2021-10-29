using HexaEngine.Extensions;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Scenes.Objects;
using System;
using System.Numerics;

namespace HexaEngine.Shaders
{
    public class LightPoint : IView
    {
        private const float DegToRadFactor = 0.0174532925f;
        private Vector3 position;
        private Vector3 lookAt;

        public Vector4 AmbientColor { get; set; }
        public Vector4 DiffuseColor { get; set; }
        public Vector3 Position { get => position; set { position = value; GenerateViewMatrix(); } }
        public Vector3 LookAt { get => lookAt; set { lookAt = value; GenerateViewMatrix(); } }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }
        public float NearPlane { get; } = 1f;
        public float FarPlane { get; } = 100f;
        public Frustum Frustum { get; set; }

        public void GenerateViewMatrix()
        {
            GenerateProjectionMatrix();

            // Create the view matrix from the three vectors.
            ViewMatrix = MatrixExtensions.LookAtLH(position, LookAt, Vector3.UnitY);
            Frustum = new Frustum(FarPlane, ProjectionMatrix, ViewMatrix);
        }

        public void GenerateProjectionMatrix()
        {
            // Setup field of view and screen aspect for a square light source.
            var fieldOfView = (float)Math.PI / 2.0f;
            var screenAspect = 1.0f;

            // Create the projection matrix for the light.
            ProjectionMatrix = MatrixExtensions.PerspectiveFovLH(fieldOfView, screenAspect, NearPlane, FarPlane);
        }
    }
}