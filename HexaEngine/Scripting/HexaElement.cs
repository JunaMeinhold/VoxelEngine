namespace HexaEngine.Scripting
{
    using HexaEngine.Scenes;
    using System.Collections.Generic;
    using System.Numerics;

    public class HexaElement
    {
        private readonly List<IComponent> components = new();

        public Matrix4x4 Transform { get; set; }

        public HexaElement Parent { get; private set; }

        public List<HexaElement> Children { get; } = new();

        public Scene Scene { get; internal set; }

        internal void InternalUpdate()
        {
            components.ForEach(x => x.Update());
            Update();
            Children.ForEach(x => x.InternalUpdate());
        }

        /// <summary>
        /// Called every frame draw.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Called every Second <seealso cref="Windows.Time.FixedUpdateRate"/> times.
        /// </summary>
        public virtual void FixedUpdate()
        {
        }

        internal void InternalAwake()
        {
            components.ForEach((x) => x.Initialize(this));
            Awake();
            Children.ForEach(x => x.InternalAwake());
        }

        /// <summary>
        /// Called when the Object is loaded by the Scene Manager.
        /// </summary>
        public virtual void Awake()
        {
        }

        internal void InternalDestroy()
        {
            components.ForEach(x => x.Uninitialize());
            Children.ForEach(x => x.InternalDestroy());
            Destroy();
        }

        /// <summary>
        /// Called when the Object is unloaded by the Scene Manager.
        /// </summary>
        public virtual void Destroy()
        {
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            foreach (var item in components)
            {
                if (item is T t)
                    return t;
            }
            return null;
        }

        public void AddComponent<T>(T t) where T : class, IComponent
        {
            components.Add(t);
        }

        public HexaElement GetParent()
        {
            return Parent;
        }

        public void SetParent(HexaElement element)
        {
            if (Parent is not null)
            {
                Parent.Children.Remove(this);
            }
            Parent = element;
            Parent.Children.Add(this);
        }

        /// <summary>
        /// Resolves the Parent of this element.
        /// </summary>
        /// <typeparam name="T">Serach Type</typeparam>
        /// <returns><typeparamref name="T"/> or null if not found.</returns>
        public T FindParent<T>() where T : HexaElement
        {
            return Parent is T t ? t : Parent is null ? null : FindParent<T>();
        }
    }
}