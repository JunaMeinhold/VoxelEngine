namespace VoxelEngine.Rendering.D3D.Interfaces
{
    using VoxelEngine.Mathematics;

    public interface IView
    {
        public CameraTransform Transform { get; }
    }
}