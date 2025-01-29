namespace VoxelEngine.Scenes
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using BepuUtilities;
    using BepuUtilities.Memory;
    using Hexa.NET.D3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Physics;
    using VoxelEngine.Physics.Characters;
    using VoxelEngine.Physics.Collidables;
    using VoxelEngine.Scripting;
    using VoxelEngine.Windows;

    public class Scene : IDisposable
    {
        private bool disposedValue;
        private bool initialized;
        private Dispatcher dispatcher;
        private SceneProfiler profiler = new();
        private List<GameObject> flatList = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        public Scene()
        {
            Elements = new(this);
        }

        /// <summary>
        /// Gets the elements.
        /// </summary>
        /// <value>
        /// The elements.
        /// </value>
        protected SceneElementCollection Elements { get; private set; }

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        /// <value>
        /// The dispatcher. Used to ensure thread-safety
        /// </value>
        public Dispatcher Dispatcher => dispatcher;

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>
        /// The window.
        /// </value>
        public GameWindow Window { get; private set; }

        /// <summary>
        /// Gets or sets the camera.
        /// </summary>
        /// <value>
        /// The camera.
        /// </value>
        public Camera Camera { get; set; }

        /// <summary>
        /// Gets or sets the renderer.
        /// </summary>
        /// <value>
        /// The renderer.
        /// </value>
        public ISceneRenderer Renderer { get; set; }

        /// <summary>
        /// Gets or sets the buffer pool.
        /// </summary>
        /// <value>
        /// The buffer pool.
        /// </value>
        public BufferPool BufferPool { get; set; }

        /// <summary>
        /// Gets the thread dispatcher.
        /// </summary>
        /// <value>
        /// The thread dispatcher.
        /// </value>
        public ThreadDispatcher ThreadDispatcher { get; private set; }

        /// <summary>
        /// Gets or sets the simulation.
        /// </summary>
        /// <value>
        /// The simulation.
        /// </value>
        public Simulation Simulation { get; private set; }

        /// <summary>
        /// Gets the character controllers.
        /// </summary>
        /// <value>
        /// The character controllers.
        /// </value>
        public CharacterControllers CharacterControllers { get; private set; }

        /// <summary>
        /// Gets the contact events.
        /// </summary>
        /// <value>
        /// The contact events.
        /// </value>
        public ContactEvents ContactEvents { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is simulating.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is simulating; otherwise, <c>false</c>.
        /// </value>
        public bool IsSimulating { get; set; }

        public SceneProfiler Profiler => profiler;

        public virtual void Initialize()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            BufferPool = new BufferPool();

            int targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
            CharacterControllers = new(BufferPool);
            ContactEvents = new ContactEvents(ThreadDispatcher, BufferPool);
            Simulation = Simulation.Create(BufferPool, new NarrowphaseCallbacks(CharacterControllers, ContactEvents), new PoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(8, 1));
            Voxels.Register(Simulation);
            Window = (GameWindow)Application.MainWindow;
            Renderer.Initialize(Window);
            Elements.ForEach(e => e.Initialize());
            initialized = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Render()
        {
            profiler.Reset();
            if (IsSimulating)
            {
                lock (Simulation)
                {
                    Simulate(Time.Delta);
                }
            }

            profiler.ProfileSimulation();

            foreach (ScriptComponent item in Elements.ScriptComponents)
            {
                item.Update();
            }

            foreach (ScriptFrameComponent item in Elements.ScriptFrameComponents)
            {
                item.Update();
            }

            foreach (IBodyComponent item in Elements.ColliderComponents)
            {
                item.Update();
            }

            profiler.ProfileUpdate();
            Camera.Transform.Recalculate();
            Renderer.Render(D3D11DeviceManager.Context.As<ID3D11DeviceContext>(), Camera, Elements);
            profiler.ProfileRender();

            Dispatcher.ExecuteQueue();
            profiler.ProfileDispatch();
        }

        private const float stepsize = 0.010f;
        private float interpol;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Simulate(float delta)
        {
            interpol += delta;
            while (interpol > stepsize)
            {
                interpol -= stepsize;
                Simulation.Timestep(stepsize, ThreadDispatcher);
            }
            ContactEvents.Flush();
        }

        /// <summary>
        /// Gets the element by T and the given name.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="name">Name</param>
        /// <returns>The element</returns>
        public T GetElementByName<T>(string name) where T : GameObject
        {
            return GetElementsByType<T>().FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Gets all elements with the type T.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>The elements</returns>
        public IEnumerable<T> GetElementsByType<T>() where T : GameObject
        {
            return Elements.Where(x => x is T).Cast<T>();
        }

        /// <summary>
        /// Gets the first element with the type T.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>The elements</returns>
        public T GetElementByType<T>() where T : GameObject
        {
            return (T)Elements.FirstOrDefault(x => x is T);
        }

        public T GetElementByComponentRef<T>(IComponent component) where T : GameObject
        {
            foreach (GameObject element in Elements)
            {
                if (element.Components.Contains(component))
                {
                    return element as T;
                }
            }

            return null;
        }

        public T GetElementByCollidableReference<T>(CollidableReference reference) where T : GameObject
        {
            foreach (IBodyComponent component in Elements.ColliderComponents)
            {
                if (component is IDynamicBodyComponent dynamic)
                {
                    if (Simulation.Bodies[dynamic.Handle].CollidableReference == reference)
                    {
                        return GetElementByComponentRef<T>(dynamic);
                    }
                }

                if (component is IStaticBodyComponent staticBody)
                {
                    if (Simulation.Statics[staticBody.Handle].CollidableReference == reference)
                    {
                        return GetElementByComponentRef<T>(staticBody);
                    }
                }
            }
            return null;
        }

        public T GetElementByBodyHandle<T>(BodyHandle handle) where T : GameObject
        {
            foreach (IBodyComponent component in Elements.ColliderComponents)
            {
                if (component is IDynamicBodyComponent dynamic)
                {
                    if (dynamic.Handle == handle)
                    {
                        return GetElementByComponentRef<T>(dynamic);
                    }
                }
            }

            return null;
        }

        public T GetElementByStaticHandle<T>(StaticHandle handle) where T : GameObject
        {
            foreach (IBodyComponent component in Elements.ColliderComponents)
            {
                if (component is IStaticBodyComponent staticBody)
                {
                    if (staticBody.Handle == handle)
                    {
                        return GetElementByComponentRef<T>(staticBody);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Adds an scene element.<br/>
        /// Calls <see cref="GameObject.Initialize"/> if <see cref="initialized"/> == <see langword="true" />
        /// </summary>
        /// <param name="sceneElement">The scene element.</param>
        public void Add(GameObject sceneElement)
        {
            if (initialized)
            {
                dispatcher.Invoke(() =>
                {
                    Elements.Add(sceneElement);
                    sceneElement.Initialize();
                });
            }
            else
            {
                Elements.Add(sceneElement);
            }
        }

        /// <summary>
        /// Removes an scene element.<br/>
        /// Calls <see cref="GameObject.Uninitialize"/> if <see cref="initialized"/> == <see langword="true" />
        /// </summary>
        /// <param name="sceneElement">The scene element.</param>
        public void Remove(GameObject sceneElement)
        {
            if (initialized)
            {
                dispatcher.Invoke(() =>
                {
                    Elements.Remove(sceneElement);
                    sceneElement.Uninitialize();
                });
            }
            else
            {
                Elements.Remove(sceneElement);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Elements.ForEach(x => x.Uninitialize());
                Elements.Dispose();
                Simulation.Clear();
                Simulation.Dispose();
                Simulation = null;
                BufferPool.Clear();
                BufferPool = null;
                Renderer.Uninitialize();
                Elements = null;
                dispatcher = null;
                Camera = null;
                Simulation = null;
                Renderer = null;
                Window = null;
                initialized = false;
                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Scene"/> class.
        /// </summary>
        ~Scene()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Register(GameObject gameObject)
        {
            flatList.Add(gameObject);
        }

        internal void Unregister(GameObject gameObject)
        {
            flatList.Remove(gameObject);
        }
    }
}