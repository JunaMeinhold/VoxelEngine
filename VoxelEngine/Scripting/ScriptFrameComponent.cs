namespace VoxelEngine.Scripting
{
    using Vortice.Direct3D11;
    using VoxelEngine.Scenes;

    /// <summary>
    /// For scripts that only need frame wise updates.
    /// </summary>
    /// <seealso cref="IComponent" />
    public abstract class ScriptFrameComponent : IComponent
    {
        /// <summary>
        /// Initializes the component.<br />
        /// Called by <see cref="SceneElement.Initialize" /> ... <see cref="Scene.Initialize" /> ... <see cref="SceneManager.Load(Scene)" /><br />
        /// Called by <see cref="SceneElement.Initialize" /> ... <see cref="Scene.Add(SceneElement)" /><see cref="Scene.initialized" /> == <see langword="true" /><br />
        /// </summary>
        /// <param name="element">The element.</param>
        public void Initialize(ID3D11Device device, SceneElement element)
        {
            Parent = element;
            Awake();
        }

        /// <summary>
        /// Uninitializes the component.<br />
        /// Called by <see cref="SceneElement.Uninitialize" /> ... <see cref="Scene.Dispose" /> ... <see cref="SceneManager.Unload" /><br />
        /// </summary>
        public void Uninitialize()
        {
            Destroy();
        }

        /// <summary>
        /// Initializes the script.<br />
        /// Called by <see cref="SceneElement.Initialize" /> ... <see cref="Scene.Initialize" /> ... <see cref="SceneManager.Load(Scene)" /><br />
        /// Called by <see cref="SceneElement.Initialize" /> ... <see cref="Scene.Add(SceneElement)" /><see cref="Scene.initialized" /> == <see langword="true" /><br />
        /// </summary>
        public abstract void Awake();

        /// <summary>
        /// Uninitializes the script.<br />
        /// Called by <see cref="SceneElement.Uninitialize" /> ... <see cref="Scene.Dispose" /> ... <see cref="SceneManager.Unload" /><br />
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        /// Called every frame draw.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Gets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public SceneElement Parent { get; private set; }

        /// <summary>
        /// Gets the scene.
        /// </summary>
        /// <value>
        /// The scene.
        /// </value>
        public Scene Scene => Parent.Scene;
    }
}