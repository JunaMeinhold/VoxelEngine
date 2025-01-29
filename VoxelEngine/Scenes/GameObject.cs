namespace VoxelEngine.Scenes
{
    using System.Numerics;
    using Hexa.NET.Mathematics;
    using Hexa.NET.D3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Mathematics;

    public class GameObject

    {
        /// <summary>
        /// Gets the scene that the element is associated.
        /// </summary>
        /// <value>
        /// The scene.
        /// </value>
        public Scene Scene { get; internal set; }

        /// <summary>
        /// Gets the dispatcher of the scene.
        /// </summary>
        /// <value>
        /// The dispatcher of the scene.
        /// </value>
        public Dispatcher Dispatcher => Scene.Dispatcher;

        /// <summary>
        /// Gets or sets the parent of the element.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public GameObject Parent { get; set; }

        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public List<GameObject> Children { get; } = new();

        /// <summary>
        /// Gets the components.
        /// </summary>
        /// <value>
        /// The components.
        /// </value>
        public List<IComponent> Components { get; } = new();

        public Transform Transform { get; protected set; } = new();

        public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Initializes this instance.<br/>
        /// Called by <see cref="Scene.Initialize"/> ... <see cref="SceneManager.Load(Scene)"/><br/>
        /// Called by <see cref="Scene.Add(GameObject)"/> if <see cref="Scene.initialized"/> == <see langword="true" /><br/>
        /// </summary>
        public virtual void Initialize()
        {
            Children.ForEach(child => child.Scene = Scene);
            Components.ForEach(component => component.Initialize(this));
            Children.ForEach(child => child.Initialize());
            Scene.Register(this);
        }

        /// <summary>
        /// Uninitializes this instance.<br/>
        /// Called by <see cref="Scene.Dispose"/> ... <see cref="SceneManager.Unload"/><br/>
        /// Called by <see cref="Scene.Remove(GameObject)"/> if <see cref="Scene.initialized"/> == <see langword="true" /><br/>
        /// </summary>
        public virtual void Uninitialize()
        {
            Scene.Unregister(this);
            Components.ForEach(component => component.Uninitialize());
            Components.Clear();
            Children.ForEach(child => child.Uninitialize());
            Children.Clear();
        }

        /// <summary>
        /// Destroys this instance. And removes it self automatically out of the scene.<br/>
        /// Calls <see cref="Scene.Remove(GameObject)"/> param <see cref="this"/><br/>
        /// </summary>
        public void Destroy()
        {
            Scene.Remove(this);
        }

        /// <summary>
        /// Binds an component to SceneElement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t">The component.</param>
        public virtual void AddComponent<T>(T t) where T : IComponent
        {
            Components.Add(t);
        }

        /// <summary>
        /// Gets an Component by T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T or null</returns>
        public virtual T GetComponent<T>() where T : IComponent
        {
            return (T)Components.FirstOrDefault(component => component is T);
        }

        /// <summary>
        /// Tries to get component by T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t">T or null</param>
        /// <returns>true if sucess, false if failed</returns>
        public virtual bool TryGetComponent<T>(out T t) where T : IComponent
        {
            T component = (T)Components.FirstOrDefault(component => component is T);
            t = component;
            return component != null;
        }

        /// <summary>
        /// Gets all T components from this instance and their Children.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetComponents<T>() where T : IComponent
        {
            IEnumerable<T> components = Components.Where(component => component is T).Cast<T>();
            foreach (T component in components)
            {
                yield return component;
            }

            foreach (GameObject child in Children)
            {
                foreach (T childComponent in child.GetComponents<T>())
                {
                    yield return childComponent;
                }
            }
        }
    }
}