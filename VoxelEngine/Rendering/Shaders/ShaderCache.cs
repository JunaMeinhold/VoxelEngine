namespace VoxelEngine.Rendering.Shaders
{
    using System.Buffers.Binary;
    using System.Text;
    using SharpGen.Runtime;
    using Vortice.Direct3D;
    using VoxelEngine.Core;

    public static class ShaderCache
    {
        private const string file = "cache/shadercache.bin";
        private static readonly Dictionary<string, KeyValuePair<DateTime, byte[]>> cache = new();

        static ShaderCache()
        {
            _ = Directory.CreateDirectory("cache");
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Load();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Save();
        }

        public static void CacheShader(string path, Blob blob)
        {
            if (!Nucleus.Settings.ShaderCache)
            {
                return;
            }

            DateTime datetime = File.GetLastWriteTime(path);
            _ = cache.Remove(path);
            cache.Add(path, new(datetime, blob.AsBytes().ToArray()));
        }

        public static bool GetShader(string path, out byte[] data, out PointerSize pointerSize)
        {
            data = default;
            pointerSize = default;
            if (!Nucleus.Settings.ShaderCache)
            {
                return false;
            }
            return false;
            if (cache.TryGetValue(path, out KeyValuePair<DateTime, byte[]> pair))
            {
                DateTime datetime = File.GetLastWriteTime(path);
                if (pair.Key == datetime)
                {
                    data = pair.Value;
                    return true;
                }
            }
            return false;
        }

        private static void Load()
        {
            if (!File.Exists(file))
            {
                return;
            }

            Span<byte> span = (Span<byte>)File.ReadAllBytes(file);
            for (int index = 0; index < span.Length;)
            {
                int filenameLength = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
                index += 4;
                string filename = Encoding.UTF8.GetString(span.Slice(index, filenameLength));
                index += filenameLength;
                DateTime timestamp = DateTime.FromFileTime(BinaryPrimitives.ReadInt64LittleEndian(span[index..]));
                index += 8;
                int dataLength = BinaryPrimitives.ReadInt32LittleEndian(span[index..]);
                index += 4;
                byte[] data = span.Slice(index, dataLength).ToArray();
                index += dataLength;
                cache.Add(filename, new(timestamp, data));
            }
        }

        private static void Save()
        {
            FileStream fs = File.Create(file);
            int size = cache.Count * 4 + cache.Sum(x => x.Key.Length) + cache.Count * 8 + cache.Count * 4 + cache.Sum(x => x.Value.Value.Length);
            Span<byte> span = new byte[size];
            int index = 0;
            foreach (KeyValuePair<string, KeyValuePair<DateTime, byte[]>> pair in cache)
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