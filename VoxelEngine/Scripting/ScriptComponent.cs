﻿namespace VoxelEngine.Scripting
{
    using Vortice.Direct3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Scenes;

    /// <summary>
    /// For scripts that need both frame wise updates and not frame wise updates.
    /// </summary>
    /// <seealso cref="IComponent" />
    public abstract class ScriptComponent : IComponent
    {
        /// <summary>
        /// Initializes the component.<br />
        /// Called by <see cref="GameObject.Initialize" /> ... <see cref="Scene.Initialize" /> ... <see cref="SceneManager.Load(Scene)" /><br />
        /// Called by <see cref="GameObject.Initialize" /> ... <see cref="Scene.Add(GameObject)" /><see cref="Scene.initialized" /> == <see langword="true" /><br />
        /// </summary>
        /// <param name="element">The element.</param>
        public void Initialize(ID3D11Device device, GameObject element)
        {
            Device = device;
            Parent = element;
            Time.FixedUpdate += Time_FixedUpdate;
            Awake();
        }

        /// <summary>
        /// Uninitializes the component.<br />
        /// Called by <see cref="GameObject.Uninitialize" /> ... <see cref="Scene.Dispose" /> ... <see cref="SceneManager.Unload" /><br />
        /// </summary>
        public void Uninitialize()
        {
            Time.FixedUpdate -= Time_FixedUpdate;
            Destroy();
        }

        /// <summary>
        /// Handles the FixedUpdate event of the Time control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Time_FixedUpdate(object sender, EventArgs e)
        {
            FixedUpdate();
        }

        /// <summary>
        /// Initializes the script.<br />
        /// Called by <see cref="GameObject.Initialize" /> ... <see cref="Scene.Initialize" /> ... <see cref="SceneManager.Load(Scene)" /><br />
        /// Called by <see cref="GameObject.Initialize" /> ... <see cref="Scene.Add(GameObject)" /><see cref="Scene.initialized" /> == <see langword="true" /><br />
        /// </summary>
        public abstract void Awake();

        /// <summary>
        /// Uninitializes the script.<br />
        /// Called by <see cref="GameObject.Uninitialize" /> ... <see cref="Scene.Dispose" /> ... <see cref="SceneManager.Unload" /><br />
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        /// Called every frame draw.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Called every <see cref="Time.FixedUpdatePerSecond"/>
        /// </summary>
        public abstract void FixedUpdate();

        /// <summary>
        /// Gets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public GameObject Parent { get; private set; }

        public ID3D11Device Device { get; private set; }

        /// <summary>
        /// Gets the scene.
        /// </summary>
        /// <value>
        /// The scene.
        /// </value>
        public Scene Scene => Parent.Scene;
    }
}