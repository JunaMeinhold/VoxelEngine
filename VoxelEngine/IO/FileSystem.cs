namespace HexaEngine.IO
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class FileSystem
    {
        private static readonly List<Asset> assetBundles = new();

        static FileSystem()
        {
            foreach (string file in Directory.GetFiles("assets/", "*.assets", SearchOption.TopDirectoryOnly))
            {
                assetBundles.AddRange(new AssetBundle(file).Assets);
            }
        }

        public static VirtualStream Open(string path)
        {
            if (File.Exists(path))
            {
                var fs = File.OpenRead(path);

                return new(fs, 0, fs.Length, true);
            }
            else
            {
                var rel = Path.GetRelativePath("assets/", path);
                var asset = assetBundles.Find(x => x.Path == rel);
#if DEBUG
                Trace.WriteLineIf(asset is null, $"Warning file {path} is missing");
#endif
                return asset?.GetStream();
            }
        }

        public static string[] ReadAllLines(string path)
        {
            var fs = Open(path);
            var reader = new StreamReader(fs);
            return reader.ReadToEnd().Split(Environment.NewLine);
        }
    }
}