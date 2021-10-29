// <copyright file="ArrayExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HexaEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class ArrayExtensions
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
            stream.Read(arr);

            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public static IEnumerable<T1> ForEachConvert<T, T1>(this IEnumerable<T> ts, Action<T> action, Converter<T, T1> converter)
        {
            foreach (var t in ts)
            {
                action.Invoke(t);
                yield return converter.Invoke(t);
            }
        }

        public static int GetIndexOfFirst<T>(this T[] ts, T state)
            where T : struct
        {
            int i = 0;
            foreach (T t in ts ?? throw new ArgumentNullException(nameof(ts)))
            {
                if (t.Equals(state))
                {
                    break;
                }

                i++;
            }

            return i;
        }

        public static float ClosestTo(this IEnumerable<float> collection, float target)
        {
            // NB Method will return int.MaxValue for a sequence containing no elements. Apply any
            // defensive coding here as necessary.
            var closest = float.MaxValue;
            var minDifference = float.MaxValue;
            foreach (var element in collection ?? throw new ArgumentNullException(nameof(collection)))
            {
                var difference = Math.Abs(element - target);
                if (minDifference > difference)
                {
                    minDifference = difference;
                    closest = element;
                }
            }

            return closest;
        }

        public static (List<float>, List<float>) SplitCoordinates(this List<Vector2> vector2s)
        {
            List<float> xs = new();
            List<float> ys = new();
            foreach (Vector2 vec in vector2s ?? throw new ArgumentNullException(nameof(vector2s)))
            {
                xs.Add(vec.X);
                ys.Add(vec.Y);
            }

            return (xs, ys);
        }

        public static float ClosestToX(this IEnumerable<Vector2> collection, float target)
        {
            // NB Method will return int.MaxValue for a sequence containing no elements. Apply any
            // defensive coding here as necessary.
            var closest = float.MaxValue;
            var minDifference = float.MaxValue;
            foreach (var element in collection ?? throw new ArgumentNullException(nameof(collection)))
            {
                var difference = Math.Abs(element.X - target);
                if (minDifference > difference)
                {
                    minDifference = difference;
                    closest = element.X;
                }
            }

            return closest;
        }

        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }

        public static T[] Add<T>(this T[] array, T value)
        {
            T[] newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[array.Length] = value;
            return newArray;
        }

        public static T[] AddToStart<T>(this T[] array, T value)
        {
            T[] newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 1);
            newArray[0] = value;
            return newArray;
        }

        public static string ArrayToString(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] ToBytes(this string str) => Encoding.UTF8.GetBytes(str);

        public static void ShrinkStream(this Stream fs, long pos, long count)
        {
            byte[] buffer = new byte[count];
            fs.Position = pos + count;
            int i = 0;
            while (fs.Read(buffer, 0, buffer.Length) != 0)
            {
                long posBef = fs.Position;
                fs.Position = pos + count * i;
                fs.Write(buffer, 0, buffer.Length);
                fs.Position = posBef;
                i++;
            }
            fs.SetLength(fs.Length - count);
        }

        public static void ExpandStream(this Stream fs, long pos, byte[] data)
        {
            ExpandStream(fs, pos, data.Length);
            fs.Position = pos;
            fs.Write(data, 0, data.Length);
        }

        public static void ExpandStream(this Stream fs, long pos, long count)
        {
            byte[] buffer = new byte[count];
            fs.Position = pos;
            fs.Read(buffer, 0, buffer.Length);
            fs.SetLength(fs.Length + count);
            fs.Position = pos;
            for (int i = 0; i < count;)
            {
                fs.Write(new byte[] { 0 }, 0, 1);
                i++;
            }

            void exp(byte[] or)
            {
                byte[] bufferx = new byte[count];
                int read = fs.Read(bufferx, 0, bufferx.Length);
                if (read == 0)
                { return; }
                fs.Position -= count;
                fs.Write(or, 0, or.Length);
                exp(bufferx);
            }
            exp(buffer);
        }

        public static T GetNext<T>(this List<T> list, T t) where T : class
        {
            var nextIndex = list.FindIndex(x => x == t) + 1;
            if (nextIndex < list.Count)
            {
                return list[nextIndex];
            }
            else
            {
                return null;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> ts, Action<T> action)
        {
            foreach (var t in ts)
            {
                action.Invoke(t);
            }
        }
    }
}