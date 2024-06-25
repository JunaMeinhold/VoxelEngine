namespace VoxelEngine.Voxel.Metadata
{
    using System.Collections.Concurrent;

    /// <summary>
    /// Factory for IBlockMetadata types. Thread-safe.
    /// </summary>
    public static class BlockMetadataFactory
    {
        private static readonly ConcurrentDictionary<BlockMetadataType, Type> types = new();
        private static readonly object _lock = new();

        public static IBlockMetadata CreateInstance(BlockMetadataType type)
        {
            Type t;
            lock (_lock)
            {
                t = types[type];
            }
            IBlockMetadata instance = (IBlockMetadata)Activator.CreateInstance(t);
            return instance;
        }

        public static T CreateInstance<T>(BlockMetadataType type) where T : IBlockMetadata, new()
        {
            Type t;
            lock (_lock)
            {
                t = types[type];
            }
            T instance = (T)(IBlockMetadata)Activator.CreateInstance(t);
            return instance;
        }

        public static bool TryCreateInstance(BlockMetadataType type, out IBlockMetadata instance)
        {
            Type t;
            lock (_lock)
            {
                if (!types.TryGetValue(type, out t))
                {
                    instance = null;
                    return false;
                }
            }
            instance = (IBlockMetadata)Activator.CreateInstance(t);
            return true;
        }

        public static bool TryCreateInstance<T>(BlockMetadataType type, out T instance) where T : IBlockMetadata, new()
        {
            Type t;
            lock (_lock)
            {
                if (!types.TryGetValue(type, out t))
                {
                    instance = default;
                    return false;
                }
            }

            IBlockMetadata metadataInstance = (IBlockMetadata)Activator.CreateInstance(t);
            if (metadataInstance is not T instanceT)
            {
                instance = default;
                return false;
            }
            instance = instanceT;
            return true;
        }

        public static Type GetTypeOf(BlockMetadataType type)
        {
            lock (_lock)
            {
                return types[type];
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                types.Clear();
            }
        }

        public static void Register<T>(BlockMetadataType type) where T : IBlockMetadata, new()
        {
            lock (_lock)
            {
                Type t = typeof(T);
                if (types.ContainsKey(type))
                {
                    types[type] = t;
                }
                else
                {
                    types.TryAdd(type, t);
                }
            }
        }

        public static void Unregister(BlockMetadataType type)
        {
            lock (_lock)
            {
                types.TryRemove(type, out _);
            }
        }

        public static bool Contains(BlockMetadataType type)
        {
            lock (_lock)
            {
                return types.ContainsKey(type);
            }
        }
    }
}