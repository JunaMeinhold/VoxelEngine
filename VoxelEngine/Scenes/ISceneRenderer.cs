namespace VoxelEngine.Scenes
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
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
        void Initialize(GameWindow window);

        /// <summary>
        /// Renders the scene with the current Camera. Called by <see cref="Scene.Render"/> ... <see cref="GameWindow.RenderVoid"/>
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="elements">The elements.</param>
        void Render(ComPtr<ID3D11DeviceContext4> context, Camera view, SceneElementCollection elements);

        void Resize(GameWindow window);

        /// <summary>
        /// Uninitializes this instance. Called by <see cref="Scene.Dispose"/> ... <see cref="SceneManager.Unload"/>
        /// </summary>
        void Uninitialize();
    }
}