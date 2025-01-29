namespace VoxelEngine.Graphics
{
    using Hexa.NET.D3D11;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Scenes;

    public interface IForwardRenderComponent : IComponent
    {
        /// <summary>
        /// Draws the SceneElement.<br/>
        /// </summary>
        /// <param name="view">The current camera.</param>
        void DrawForward(ComPtr<ID3D11DeviceContext> context);

        BoundingBox GetBoundingBox();
    }
}