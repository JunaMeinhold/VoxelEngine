namespace VoxelEngine.Graphics
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Scenes;

    public abstract class BaseRenderComponent : IRenderComponent
    {
        public GameObject GameObject { get; set; }

        public abstract int QueueIndex { get; }

        public abstract void Awake();

        public abstract void Destroy();

        public abstract void Draw(ComPtr<ID3D11DeviceContext> context, PassIdentifer pass, Camera camera, object? parameter);
    }
}