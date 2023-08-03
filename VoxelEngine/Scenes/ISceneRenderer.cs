namespace VoxelEngine.Scenes
{
    using Vortice.Direct3D11;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Windows;

    /// <summary>
    /// The interface for render callbacks.
    /// </summary>
    public interface ISceneRenderer
    {
        /// <summary>
        /// Initializes this instance. Called by <see cref="Scene.Initialize"/> ... <see cref="SceneManager.Load(Scene)"/>
        /// </summary>
        void Initialize(ID3D11Device device, GameWindow window);

        /// <summary>
        /// Renders the scene with the current Camera. Called by <see cref="Scene.Render"/> ... <see cref="GameWindow.RenderVoid"/>
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="elements">The elements.</param>
        void Render(ID3D11DeviceContext context, Camera view, SceneElementCollection elements);

        void Resize(ID3D11Device device, GameWindow window);

        /// <summary>
        /// Uninitializes this instance. Called by <see cref="Scene.Dispose"/> ... <see cref="SceneManager.Unload"/>
        /// </summary>
        void Uninitialize();
    }
}