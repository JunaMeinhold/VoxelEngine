namespace VoxelEngine.Scenes
{
    using Hexa.NET.Mathematics;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Windows;

    public interface IForwardRenderComponent : IComponent
    {
        /// <summary>
        /// Draws the SceneElement.<br/>
        /// Called by <seealso cref="ISceneRenderer.Render(IView, SceneElementCollection)"/> ... <seealso cref="Scene.Render"/> ... <seealso cref="GameWindow.RenderVoid"/><br/>
        /// </summary>
        /// <param name="view">The current camera.</param>
        void DrawForward(ID3D11DeviceContext context, IView view);

        BoundingBox GetBoundingBox();
    }
}