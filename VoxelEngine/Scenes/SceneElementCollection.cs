namespace VoxelEngine.Scenes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using VoxelEngine;
    using VoxelEngine.Physics;
    using VoxelEngine.Scripting;

    public class SceneElementCollection : IList<GameObject>, IDisposable
    {
        private Scene parent;
        private List<GameObject> elements = new();
        private List<IForwardRenderComponent> forwardComponents = new();
        private List<IDeferredRenderComponent> deferredComponents = new();
        private List<IDepthRenderComponent> depthComponents = new();
        private List<IBodyComponent> colliderComponents = new();
        private List<ScriptComponent> scriptComponents = new();
        private List<ScriptFixedComponent> scriptFixedComponents = new();
        private List<ScriptFrameComponent> scriptFrameComponents = new();
        private List<IDirectionalLightComponent> directionalLightsComponents = new();
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneElementCollection"/> class.<br/>
        /// Automatically sets <paramref name="parent"/> for all <see cref="GameObject.Scene"/>
        /// </summary>
        /// <param name="parent">The parent scene.</param>
        public SceneElementCollection(Scene parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Gets the forward components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The forward components.
        /// </value>
        public IReadOnlyList<IForwardRenderComponent> ForwardComponents => forwardComponents;

        /// <summary>
        /// Gets the deferred components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The deferred components.
        /// </value>
        public IReadOnlyList<IDeferredRenderComponent> DeferredComponents => deferredComponents;

        /// <summary>
        /// Gets the depth components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The deferred components.
        /// </value>
        public IReadOnlyList<IDepthRenderComponent> DepthComponents => depthComponents;

        /// <summary>
        /// Gets the collider components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The collider components.
        /// </value>
        public IReadOnlyList<IBodyComponent> ColliderComponents => colliderComponents;

        /// <summary>
        /// Gets the script components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The script components.
        /// </value>
        public IReadOnlyList<ScriptComponent> ScriptComponents => scriptComponents;

        /// <summary>
        /// Gets the script fixed components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The script fixed components.
        /// </value>
        public IReadOnlyList<ScriptFixedComponent> ScriptFixedComponents => scriptFixedComponents;

        /// <summary>
        /// Gets the script frame components. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The script frame components.
        /// </value>
        public IReadOnlyList<ScriptFrameComponent> ScriptFrameComponents => scriptFrameComponents;

        /// <summary>
        /// Gets the directional lights. Pre-Cached for high performance.
        /// </summary>
        /// <value>
        /// The directional lights.
        /// </value>
        public IReadOnlyList<IDirectionalLightComponent> DirectionalLights => directionalLightsComponents;

        /// <summary>
        /// Gets or sets the <see cref="GameObject"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="GameObject"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public GameObject this[int index] { get => ((IList<GameObject>)elements)[index]; set => ((IList<GameObject>)elements)[index] = value; }

        /// <summary>
        /// Executes an <paramref name="action"/> foreach element in this collection.
        /// </summary>
        /// <param name="action">The action.</param>
        public void ForEach(Action<GameObject> action)
        {
            foreach (GameObject element in elements)
            {
                action(element);
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public int Count => ((ICollection<GameObject>)elements).Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        public bool IsReadOnly => ((ICollection<GameObject>)elements).IsReadOnly;

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="ArgumentNullException">item</exception>
        /// <exception cref="ArgumentException">Element is already in a scene</exception>
        public void Add(GameObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Parent != null)
            {
                throw new ArgumentException("Element is already in a scene");
            }

            forwardComponents.AddAllComponents(item);
            deferredComponents.AddAllComponents(item);
            depthComponents.AddAllComponents(item);
            colliderComponents.AddAllComponents(item);
            scriptComponents.AddAllComponents(item);
            scriptFixedComponents.AddAllComponents(item);
            scriptFrameComponents.AddAllComponents(item);
            directionalLightsComponents.AddAllComponents(item);

            item.Scene = parent;
            ((ICollection<GameObject>)elements).Add(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            forwardComponents.Clear();
            deferredComponents.Clear();
            depthComponents.Clear();
            colliderComponents.Clear();
            scriptComponents.Clear();
            scriptFixedComponents.Clear();
            scriptFrameComponents.Clear();
            directionalLightsComponents.Clear();
            ((ICollection<GameObject>)elements).Clear();
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(GameObject item)
        {
            return ((ICollection<GameObject>)elements).Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(GameObject[] array, int arrayIndex)
        {
            ((ICollection<GameObject>)elements).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<GameObject> GetEnumerator()
        {
            return ((IEnumerable<GameObject>)elements).GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>
        /// The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(GameObject item)
        {
            return ((IList<GameObject>)elements).IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <exception cref="ArgumentNullException">item</exception>
        /// <exception cref="ArgumentException">Element is already in a scene</exception>
        public void Insert(int index, GameObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Parent != null)
            {
                throw new ArgumentException("Element is already in a scene");
            }

            forwardComponents.AddAllComponents(item);
            deferredComponents.AddAllComponents(item);
            depthComponents.AddAllComponents(item);
            colliderComponents.AddAllComponents(item);
            scriptComponents.AddAllComponents(item);
            scriptFixedComponents.AddAllComponents(item);
            scriptFrameComponents.AddAllComponents(item);
            directionalLightsComponents.AddAllComponents(item);
            item.Scene = parent;
            ((IList<GameObject>)elements).Insert(index, item);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        public bool Remove(GameObject item)
        {
            item.Parent = null;
            forwardComponents.RemoveAllComponents(item);
            deferredComponents.RemoveAllComponents(item);
            depthComponents.RemoveAllComponents(item);
            colliderComponents.RemoveAllComponents(item);
            scriptComponents.RemoveAllComponents(item);
            scriptFixedComponents.RemoveAllComponents(item);
            scriptFrameComponents.RemoveAllComponents(item);
            directionalLightsComponents.RemoveAllComponents(item);

            return ((ICollection<GameObject>)elements).Remove(item);
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            GameObject item = elements[index];
            item.Parent = null;
            forwardComponents.RemoveAllComponents(item);
            deferredComponents.RemoveAllComponents(item);
            depthComponents.RemoveAllComponents(item);
            colliderComponents.RemoveAllComponents(item);
            scriptComponents.RemoveAllComponents(item);
            scriptFixedComponents.RemoveAllComponents(item);
            scriptFrameComponents.RemoveAllComponents(item);
            directionalLightsComponents.RemoveAllComponents(item);

            ((IList<GameObject>)elements).RemoveAt(index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)elements).GetEnumerator();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                parent = null;
                elements.Clear();
                elements = null;
                forwardComponents.Clear();
                forwardComponents = null;
                deferredComponents.Clear();
                deferredComponents = null;
                depthComponents.Clear();
                depthComponents = null;
                colliderComponents.Clear();
                colliderComponents = null;
                scriptComponents.Clear();
                scriptComponents = null;
                scriptFixedComponents.Clear();
                scriptFixedComponents = null;
                scriptFrameComponents.Clear();
                scriptFrameComponents = null;
                directionalLightsComponents.Clear();
                directionalLightsComponents = null;
                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SceneElementCollection"/> class.
        /// </summary>
        ~SceneElementCollection()
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
    }
}