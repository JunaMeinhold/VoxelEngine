namespace VoxelEngine.Scenes
{
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Windows;

    public interface IDepthRenderComponent : IComponent
    {
        /// <summary>
        /// Draws the SceneElement.<br/>
        /// Called by <seealso cref="ISceneRenderer.Render(IView, SceneElementCollection)"/> ... <seealso cref="Scene.Render"/> ... <seealso cref="GameWindow.RenderVoid"/><br/>
        /// </summary>
        /// <param name="view">The current camera.</param>
        void DrawDepth(ID3D11DeviceContext context, IRenderTarget target, IView view, IView light);
    }
}