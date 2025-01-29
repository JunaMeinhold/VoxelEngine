namespace VoxelEngine.Scenes
{
    using Hexa.NET.D3D11;
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Windows.Events;
    using VoxelEngine.Graphics.D3D11.Interfaces;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    public class Camera : GameObject, IView
    {
        public new CameraTransform Transform;
        private bool autoSize = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera"/> class.
        /// </summary>
        public Camera()
        {
            base.Transform = Transform = new();
        }

        public ProjectionType ProjectionType { get => Transform.ProjectionType; set => Transform.ProjectionType = value; }

        public float Fov { get => Transform.Fov; set => Transform.Fov = value; }

        public float Far { get => Transform.Far; set => Transform.Far = value; }

        public float Near { get => Transform.Near; set => Transform.Near = value; }

        public bool AutoSize { get => autoSize; set => autoSize = value; }

        public float Width { get => Transform.Width; set => Transform.Width = value; }

        public float Height { get => Transform.Height; set => Transform.Height = value; }

        CameraTransform IView.Transform => Transform;

        public static Camera Current => SceneManager.Current.Camera;

        public override void Awake()
        {
            Application.MainWindow.Resized += Resized;
            if (Application.MainWindow != null)
                Application.MainWindow.Resized += Resized;
            base.Awake();
            if (!autoSize || Application.MainWindow == null) return;
            Transform.Width = Application.MainWindow.Width;
            Transform.Height = Application.MainWindow.Height;
        }

        public override void Destroy()
        {
            if (Application.MainWindow != null)
                Application.MainWindow.Resized -= Resized;
            base.Destroy();
        }

        private void Resized(object? sender, ResizedEventArgs e)
        {
            if (!autoSize) return;
            Transform.Width = e.NewWidth;
            Transform.Height = e.NewHeight;
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
            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = screenDim;
        }

        public CBCamera(Camera camera, Viewport screenDim)
        {
            Proj = Matrix4x4.Transpose(camera.Transform.Projection);
            View = Matrix4x4.Transpose(camera.Transform.View);
            ProjInv = Matrix4x4.Transpose(camera.Transform.ProjectionInv);
            ViewInv = Matrix4x4.Transpose(camera.Transform.ViewInv);
            ViewProj = Matrix4x4.Transpose(camera.Transform.ViewProjection);
            ViewProjInv = Matrix4x4.Transpose(camera.Transform.ViewProjectionInv);
            PrevViewProj = Matrix4x4.Transpose(camera.Transform.PrevViewProjection);
            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = new(screenDim.Width, screenDim.Height);
        }

        public CBCamera(Camera camera, Hexa.NET.D3D11.Viewport screenDim)
        {
            Proj = Matrix4x4.Transpose(camera.Transform.Projection);
            View = Matrix4x4.Transpose(camera.Transform.View);
            ProjInv = Matrix4x4.Transpose(camera.Transform.ProjectionInv);
            ViewInv = Matrix4x4.Transpose(camera.Transform.ViewInv);
            ViewProj = Matrix4x4.Transpose(camera.Transform.ViewProjection);
            ViewProjInv = Matrix4x4.Transpose(camera.Transform.ViewProjectionInv);
            PrevViewProj = Matrix4x4.Transpose(camera.Transform.PrevViewProjection);
            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = new(screenDim.Width, screenDim.Height);
        }

        public CBCamera(Camera camera, Vector2 screenDim, Matrix4x4 last)
        {
            Proj = Matrix4x4.Transpose(camera.Transform.Projection);
            View = Matrix4x4.Transpose(camera.Transform.View);
            ProjInv = Matrix4x4.Transpose(camera.Transform.ProjectionInv);
            ViewInv = Matrix4x4.Transpose(camera.Transform.ViewInv);
            ViewProj = Matrix4x4.Transpose(camera.Transform.ViewProjection);
            ViewProjInv = Matrix4x4.Transpose(camera.Transform.ViewProjectionInv);
            PrevViewProj = Matrix4x4.Transpose(last);
            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = screenDim;
        }

        public CBCamera(Camera camera, Vector2 screenDim, CBCamera last)
        {
            Proj = Matrix4x4.Transpose(camera.Transform.Projection);
            View = Matrix4x4.Transpose(camera.Transform.View);
            ProjInv = Matrix4x4.Transpose(camera.Transform.ProjectionInv);
            ViewInv = Matrix4x4.Transpose(camera.Transform.ViewInv);
            ViewProj = Matrix4x4.Transpose(camera.Transform.ViewProjection);
            ViewProjInv = Matrix4x4.Transpose(camera.Transform.ViewProjectionInv);
            PrevViewProj = last.ViewProj;
            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = screenDim;
        }

        public CBCamera(CameraTransform camera, Vector2 screenDim)
        {
            Proj = Matrix4x4.Transpose(camera.Projection);
            View = Matrix4x4.Transpose(camera.View);
            ProjInv = Matrix4x4.Transpose(camera.ProjectionInv);
            ViewInv = Matrix4x4.Transpose(camera.ViewInv);
            ViewProj = Matrix4x4.Transpose(camera.ViewProjection);
            ViewProjInv = Matrix4x4.Transpose(camera.ViewProjectionInv);
            PrevViewProj = Matrix4x4.Transpose(camera.PrevViewProjection);
            Far = camera.Far;
            Near = camera.Near;
            ScreenDim = screenDim;
        }
    }
}