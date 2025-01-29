namespace VoxelEngine.Scenes
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;

    public interface IRenderComponent : IComponent
    {
        public int QueueIndex { get; }

        public void Draw(ComPtr<ID3D11DeviceContext> context, PassIdentifer pass, Camera camera, object? parameter);
    }
}