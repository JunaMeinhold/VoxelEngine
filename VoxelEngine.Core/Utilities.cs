namespace VoxelEngine.Core
{
    using VoxelEngine.Core.Unsafes;

    public static unsafe class Utilities
    {
        public static T* AsPointer<T>(T[] values) where T : unmanaged
        {
            fixed (T* ptr = values)
            {
                return ptr;
            }
        }

        public static T** AsPointer<T>(T*[] value) where T : unmanaged
        {
            fixed (T** ptr = value)
            {
                return ptr;
            }
        }

        public static T* AsPointer<T>(T value) where T : unmanaged
        {
            fixed (T* ptr = new T[] { value })
            {
                return ptr;
            }
        }

        public static T** AsPointer<T>(Pointer<T>[] pointers) where T : unmanaged
        {
            T*[] ts = new T*[pointers.Length];
            for (int i = 0; i < pointers.Length; i++)
            {
                ts[i] = pointers[i];
            }

            return AsPointer(ts);
        }

        public static T** AsPointer<T>(T[][] pointers) where T : unmanaged
        {
            T*[] ts = new T*[pointers.Length];
            for (int i = 0; i < pointers.Length; i++)
            {
                ts[i] = AsPointer(pointers[i]);
            }

            return AsPointer(ts);
        }
    }
}