﻿namespace VoxelEngine.Scenes
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using VoxelEngine.Core;
    using VoxelEngine.Windows;

    /// <summary>
    /// Contains the <see cref="Current"/> scene and informs about a change (<see cref="SceneChanged"/>) of <see cref="Current"/>
    /// </summary>
    public static class SceneManager
    {
        /// <summary>
        /// Gets the current scene.
        /// </summary>
        /// <value>
        /// The current scene.
        /// </value>
        public static Scene Current { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; [MethodImpl(MethodImplOptions.AggressiveInlining)] private set; }

        public static readonly Lock Lock = new();

        /// <summary>
        /// Occurs when [scene changed].
        /// </summary>
        public static event EventHandler SceneChanged;

        /// <summary>
        /// Loads the specified scene and disposes the old Scene automatically.<br/>
        /// Calls <see cref="Scene.Initialize"/> from <paramref name="scene"/><br/>
        /// Calls <see cref="Scene.Dispose"/> if <see cref="Current"/> != <see langword="null"/><br/>
        /// Notifies <see cref="SceneChanged"/><br/>
        /// Forces the GC to Collect.<br/>
        /// </summary>
        /// <param name="scene">The scene.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Load(Scene scene)
        {
            (Application.MainWindow as GameWindow).RenderDispatcher.Invoke(() =>
            {
                scene.Initialize();
                if (Current == null)
                {
                    Current = scene;
                    SceneChanged?.Invoke(null, EventArgs.Empty);
                    return;
                }
                lock (Lock)
                {
                    Current?.Dispose();
                    Current = scene;
                    SceneChanged?.Invoke(null, EventArgs.Empty);
                }
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            });
        }

        /// <summary>
        /// Asynchronouses loads the scene over <see cref="Load(Scene)"/>
        /// </summary>
        /// <param name="scene">The scene.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task AsyncLoad(Scene scene)
        {
            return Task.Factory.StartNew(() =>
            {
                Load(scene);
            });
        }

        /// <summary>
        /// Unloads <see cref="Current"/> if != <see langword="null"/><br/>
        /// Sets <see cref="Current"/> <see langword="null"/><br/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Unload()
        {
            Current?.Dispose();
            Current = null;
        }
    }
}