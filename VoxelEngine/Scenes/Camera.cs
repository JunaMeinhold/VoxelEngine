namespace VoxelEngine.Scenes
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Events;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.DXGI;

    public class Camera : SceneElement, IView
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

        public override void Initialize(ID3D11Device device)
        {
            Application.MainWindow.Resized += Resized;
            if (Application.MainWindow != null)
                Application.MainWindow.Resized += Resized;
            base.Initialize(device);
            if (!autoSize || Application.MainWindow == null) return;
            Transform.Width = Application.MainWindow.Width;
            Transform.Height = Application.MainWindow.Height;
        }

        public override void Uninitialize()
        {
            if (Application.MainWindow != null)
                Application.MainWindow.Resized -= Resized;
            base.Uninitialize();
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

    public struct CameraData
    {
        public CameraData(CameraTransform transform)
        {
            Position = transform.Position;
            reserved = 0;
            View = Matrix4x4.Transpose(transform.View);
            Proj = Matrix4x4.Transpose(transform.Projection);
        }

        public CameraData(Vector3 position, Matrix4x4 view, Matrix4x4 proj)
        {
            Position = position;
            reserved = 0;
            View = Matrix4x4.Transpose(view);
            Proj = Matrix4x4.Transpose(proj);
        }

        public Vector3 Position;
        public float reserved;
        public Matrix4x4 View;
        public Matrix4x4 Proj;
    }
}