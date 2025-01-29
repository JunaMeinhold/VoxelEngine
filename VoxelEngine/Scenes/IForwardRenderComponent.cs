namespace VoxelEngine.Scenes
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.D3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Windows;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11.Interfaces;

    public interface IForwardRenderComponent : IComponent
    {
        /// <summary>
        /// Draws the SceneElement.<br/>
        /// Called by <seealso cref="ISceneRenderer.Render(IView, SceneElementCollection)"/> ... <seealso cref="Scene.Render"/> ... <seealso cref="GameWindow.RenderVoid"/><br/>
        /// </summary>
        /// <param name="view">The current camera.</param>
        void DrawForward(ComPtr<ID3D11DeviceContext> context, IView view);

        BoundingBox GetBoundingBox();
    }
}