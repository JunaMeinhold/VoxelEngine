namespace VoxelEngine.Graphics.D3D11.Interfaces
{
    using Hexa.NET.Mathematics;

    public interface IView
    {
        public CameraTransform Transform { get; }
    }
}