namespace VoxelEngine.Voxel
{
    using System;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;

    public static class Extensions
    {
        /// <summary>
        /// Mappt einen Vector2 zu Chunk Indexen.
        /// </summary>
        /// <param name="vector">Position</param>
        /// <param name="width">Chunk width</param>
        /// <returns>Index</returns>
        public static int MapToIndex(this Vector2 vector, int width)
        {
            return (int)(vector.X + vector.Y * width);
        }

        /// <summary>
        /// Mappt einen Vector2 zu Chunk Indexen.
        /// </summary>
        /// <param name="vector">Position</param>
        /// <param name="width">Chunk width</param>
        /// <param name="depth">Chunk depth</param>
        /// <returns>Index</returns>
        public static int MapToIndex(this Vector3 vector, int width, int depth)
        {
            return (int)(depth * width * vector.Z + width * vector.X + vector.Y);
        }

        /// <summary>
        /// Mappt xyz werte zu Chunk Indexen.
        /// </summary>
        /// <param name="x">Position X</param>
        /// <param name="y">Position Y</param>
        /// <param name="z">Position Z</param>
        /// <param name="width">Chunk width</param>
        /// <param name="depth">Chunk depth</param>
        /// <returns>Index</returns>
        public static int MapToIndex(float x, float y, float z, int width, int depth)
        {
            return (int)(depth * width * z + width * x + y);
        }

        /// <summary>
        /// Wandelt ein unmanaged struct in eine Byte array um.
        /// </summary>
        /// <typeparam name="T">unmanaged struct</typeparam>
        /// <param name="str">T</param>
        /// <returns>byte[]</returns>
        public static byte[] GetBytes<T>(this T str) where T : unmanaged
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        /// <summary>
        /// Wandelt ein unmanaged struct um und speichert die Bytes in die Ziel-Array
        /// </summary>
        /// <typeparam name="T">unmanaged struct</typeparam>
        /// <param name="str">T</param>
        /// <param name="dest">Ziel-Array</param>
        /// <returns>Ziel-Array</returns>
        public static byte[] GetBytes<T>(this T str, byte[] dest) where T : unmanaged
        {
            int size = Marshal.SizeOf(str);

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, dest, 0, size);
            Marshal.FreeHGlobal(ptr);
            return dest;
        }

        /// <summary>
        /// Gibt die Größe in bytes eines unmanaged struct zurück.
        /// </summary>
        /// <typeparam name="T">unmanaged struct</typeparam>
        /// <param name="str">T</param>
        /// <returns>count bytes</returns>
        public static int GetSize<T>(this T str) where T : unmanaged
        {
            return Marshal.SizeOf(str);
        }

        public static T FromBytes<T>(this byte[] arr) where T : unmanaged
        {
            T str = new();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public static T FromStream<T>(this Stream stream) where T : unmanaged
        {
            T str = new();

            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];
            _ = stream.Read(arr);

            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
    }
}