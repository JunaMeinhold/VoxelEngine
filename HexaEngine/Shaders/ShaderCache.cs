using HexaEngine.Windows;
using SharpGen.Runtime;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D;

namespace HexaEngine.Shaders
{
    public static class ShaderCache
    {
        private const string file = "cache/shadercache.bin";
        private static readonly Dictionary<string, KeyValuePair<DateTime, byte[]>> cache = new();

        static ShaderCache()
        {
            Directory.CreateDirectory("cache");
            Load();
            Application.ApplicationClosing += Application_ApplicationClosing;
        }

        public static bool ForceReCompile { get; set; } = false;

        private static void Application_ApplicationClosing(object sender, EventArgs e)
        {
            Save();
        }

        public static void CacheShader(string path, Blob blob)
        {
            var datetime = File.GetLastWriteTime(path);
            cache.Remove(path);
            cache.Add(path, new(datetime, blob.GetBytes()));
        }

        public static bool GetShader(string path, out IntPtr pointer, out PointerSize pointerSize)
        {
            pointer = default;
            pointerSize = default;
            if (ForceReCompile) return false;
            if (cache.TryGetValue(path, out var pair))
            {
                var datetime = File.GetLastWriteTime(path);
                if (pair.Key == datetime)
                {
                    pointer = Marshal.AllocHGlobal(pair.Value.Length);
                    Marshal.Copy(pair.Value, 0, pointer, pair.Value.Length);
                    pointerSize = new PointerSize(pair.Value.Length);
                    return true;
                }
            }
            return false;
        }

        public static void FreePointer(IntPtr pointer)
        {
            Marshal.FreeHGlobal(pointer);
        }

        private static void Load()
        {
            if (!File.Exists(file)) return;
            var span = (Span<byte>)File.ReadAllBytes(file);
            for (int index = 0; index < span.Length;)
            {
                var filenameLength = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
                index += 4;
                var filename = Encoding.UTF8.GetString(span.Slice(index, filenameLength));
                index += filenameLength;
                var timestamp = DateTime.FromFileTime(BinaryPrimitives.ReadInt64LittleEndian(span[index..]));
                index += 8;
                var dataLength = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
                index += 4;
                var data = span.Slice(index, dataLength).ToArray();
                index += dataLength;
                cache.Add(filename, new(timestamp, data));
            }
        }

        private static void Save()
        {
            var fs = File.Create(file);
            var size = cache.Count * 4 + cache.Sum(x => x.Key.Length) + cache.Count * 8 + cache.Count * 4 + cache.Sum(x => x.Value.Value.Length);
            Span<byte> span = new byte[size];
            var index = 0;
            foreach (var pair in cache)
            {
                BinaryPrimitives.WriteInt32LittleEndian(span[index..], pair.Key.Length);
                index += 4;
                Encoding.UTF8.GetBytes(pair.Key).CopyTo(span[index..]);
                index += pair.Key.Length;
                BinaryPrimitives.WriteInt64LittleEndian(span[index..], pair.Value.Key.ToFileTime());
                index += 8;
                BinaryPrimitives.WriteInt32LittleEndian(span[index..], pair.Value.Value.Length);
                index += 4;
                pair.Value.Value.CopyTo(span[index..]);
                index += pair.Value.Value.Length;
            }
            fs.Write(span);
            fs.Flush();
            fs.Close();
        }
    }
}