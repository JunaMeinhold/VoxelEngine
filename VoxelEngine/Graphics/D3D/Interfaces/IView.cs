namespace VoxelEngine.Rendering.D3D.Interfaces
{
    using Hexa.NET.Mathematics;

    public interface IView
    {
        public CameraTransform Transform { get; }
    }
}