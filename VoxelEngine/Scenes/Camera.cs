namespace VoxelEngine.Scenes
{
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Windows.Events;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    public class Camera : GameObject
    {
        public new CameraTransform Transform = new();
        private bool autoSize = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera"/> class.
        /// </summary>
        public Camera()
        {
            OverwriteTransform(Transform);
        }

        public ProjectionType ProjectionType { get => Transform.ProjectionType; set => Transform.ProjectionType = value; }

        public float Fov { get => Transform.Fov; set => Transform.Fov = value; }

        public float Far { get => Transform.Far; set => Transform.Far = value; }

        public float Near { get => Transform.Near; set => Transform.Near = value; }

        public static Camera Current => SceneManager.Current.Camera;

        public override void Awake()
        {
            base.Awake();
            if (!autoSize || Application.MainWindow == null) return;
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public static implicit operator CameraTransform(Camera camera)
        {
            return camera.Transform;
        }
    }

    public struct CBWorld
    {
        public Matrix4x4 World;
        public Matrix4x4 WorldInv;

        public CBWorld(Matrix4x4 transform)
        {
            Matrix4x4.Invert(transform, out var inverse);
            World = Matrix4x4.Transpose(transform);
            WorldInv = Matrix4x4.Transpose(inverse);
        }

        public CBWorld(Transform transform)
        {
            Matrix4x4.Invert(transform.Global, out var inverse);
            World = Matrix4x4.Transpose(transform.Global);
            WorldInv = Matrix4x4.Transpose(inverse);
        }
    }

    public struct CBCamera
    {
        public Matrix4x4 View;
        public Matrix4x4 Proj;
        public Matrix4x4 ViewInv;
        public Matrix4x4 ProjInv;
        public Matrix4x4 ViewProj;
        public Matrix4x4 ViewProjInv;
        public Matrix4x4 RelViewProj;
        public Matrix4x4 RelViewProjInv;
        public Matrix4x4 PrevViewProj;

        public float Far;
        public float Near;
        public Vector2 ScreenDim;

        public CBCamera(Camera camera, Vector2 screenDim)
        {
            Proj = Matrix4x4.Transpose(camera.Transform.Projection);
            View = Matrix4x4.Transpose(camera.Transform.View);
            ProjInv = Matrix4x4.Transpose(camera.Transform.ProjectionInv);
            ViewInv = Matrix4x4.Transpose(camera.Transform.ViewInv);
            ViewProj = Matrix4x4.Transpose(camera.Transform.ViewProjection);
            ViewProjInv = Matrix4x4.Transpose(camera.Transform.ViewProjectionInv);
            PrevViewProj = Matrix4x4.Transpose(camera.Transform.PrevViewProjection);

            Matrix4x4 view = camera.Transform.View;
            view[3, 0] = 0;
            view[3, 1] = 0;
            view[3, 2] = 0;

            RelViewProj = view * camera.Transform.Projection;
            Matrix4x4.Invert(RelViewProj, out RelViewProjInv);

            RelViewProj = Matrix4x4.Transpose(RelViewProj);
            RelViewProjInv = Matrix4x4.Transpose(RelViewProjInv);

            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = screenDim;
        }
    }
}