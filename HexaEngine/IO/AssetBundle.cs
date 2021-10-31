namespace HexaEngine.IO
{
    using System.IO;

    public class AssetBundle
    {
        private readonly Stream stream;

        public AssetBundle(string path)
        {
            var fs = File.OpenRead(path);
            var count = fs.ReadInt();
            Assets = new Asset[count];
            for (int i = 0; i < count; i++)
            {
                var apath = fs.ReadString();
                var length = fs.ReadInt64();
                var pointer = fs.Position;
                fs.Position += length;
                Assets[i] = new Asset() { Path = apath, Pointer = pointer, Length = length, Bundle = this };
            }
            stream = fs;
        }

        public Asset[] Assets { get; }

        public Stream GetStream() => stream;
    }
}