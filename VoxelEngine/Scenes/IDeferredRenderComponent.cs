namespace VoxelEngine.Scenes
{
    using BepuUtilities;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Windows;

    public interface IDeferredRenderComponent : IComponent
    {
        /// <summary>
        /// Draws the SceneElement.<br/>
        /// Called by <seealso cref="ISceneRenderer.Render(IView, SceneElementCollection)"/> ... <seealso cref="Scene.Render"/> ... <seealso cref="GameWindow.RenderVoid"/><br/>
        /// </summary>
        /// <param name="view">The current camera.</param>
        void DrawDeferred(ID3D11DeviceContext context, IRenderTarget target, IView view);

        BoundingBox GetBoundingBox();
    }
}