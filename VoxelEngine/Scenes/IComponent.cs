namespace VoxelEngine.Scenes
{
    using Hexa.NET.D3D11;

    /// <summary>
    /// Interface for SceneElement Components
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Initializes the component.<br/>
        /// Called by <see cref="GameObject.Initialize"/> ... <see cref="Scene.Initialize"/> ... <see cref="SceneManager.Load(Scene)"/><br/>
        /// Called by <see cref="GameObject.Initialize"/> ... <see cref="Scene.Add(GameObject)"/> <see cref="Scene.initialized"/> == <see langword="true" /><br/>
        /// </summary>
        /// <param name="element">The element.</param>
        void Initialize(GameObject element);

        /// <summary>
        /// Uninitializes the component.<br/>
        /// Called by <see cref="GameObject.Uninitialize"/> ... <see cref="Scene.Dispose"/> ... <see cref="SceneManager.Unload"/><br/>
        /// </summary>
        void Uninitialize();
    }
}