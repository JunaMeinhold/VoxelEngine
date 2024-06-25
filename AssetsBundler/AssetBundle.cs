namespace AssetsBundler
{
    using System;
    using System.IO;

    public class AssetBundle
    {
        public AssetBundle(string path)
        {
            FileStream fs = File.OpenRead(path);
            int count = fs.ReadInt();
            Assets = new Asset[count];
            for (int i = 0; i < count; i++)
            {
                string apath = fs.ReadString();
                long length = fs.ReadInt64();
                byte[] data = fs.Read(length);
                Assets[i] = new Asset() { Path = apath, Data = data };
            }
        }

        public Asset[] Assets { get; }

        public void Extract(string path)
        {
            DirectoryInfo root = new(path);
            foreach (Asset asset in Assets)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(root.FullName + asset.Path));
                FileStream fs = File.Create(root.FullName + asset.Path);
                fs.Write(asset.Data);
                fs.Flush();
                fs.Close();
                fs.Dispose();
            }
        }

        public static void CreateFrom(string path)
        {
            DirectoryInfo root = new(path);

            int i = 0;
            foreach (DirectoryInfo dir in root.GetDirectories())
            {
                FileStream fs = File.Create(dir.Name + ".assets");
                fs.Position = 4; foreach (FileInfo file in root.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    if (file.Extension == ".assets")
                    {
                        continue;
                    }

                    FileStream ts = file.OpenRead();
                    fs.WriteString(Path.GetRelativePath(path, file.FullName));
                    fs.WriteInt64(ts.Length);
                    ts.CopyTo(fs);
                    i++;
                }
                fs.Position = 0;
                fs.WriteInt(i);
                fs.Flush();
                fs.Close();
            }
        }

        public static void GenerateFrom(string path)
        {
            DirectoryInfo root = new(path);

            foreach (DirectoryInfo dir in root.GetDirectories())
            {
                int i = 0;
                string filename = dir.Name + ".assets";
                FileStream fs = File.Create(root.FullName + dir.Name + ".assets");
                fs.Position = 4;
                foreach (FileInfo file in dir.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    if (file.Extension == ".assets")
                    {
                        continue;
                    }

                    FileStream ts = file.OpenRead();
                    string rel = Path.GetRelativePath(path, file.FullName);
                    Console.WriteLine($"Packing {filename} <-- {rel}");
                    fs.WriteString(rel);
                    fs.WriteInt64(ts.Length);
                    ts.CopyTo(fs);
                    ts.Close();
                    i++;
                }
                dir.Delete(true);
                fs.Position = 0;
                fs.WriteInt(i);
                fs.Flush();
                fs.Close();
            }
        }

        public static void GenerateFrom2(string path)
        {
            DirectoryInfo root = new(path);

            foreach (DirectoryInfo dir in root.GetDirectories())
            {
                int i = 0;
                string filename = dir.Name + ".assets";
                FileStream fs = File.Create(root.FullName + dir.Name + ".assets");
                fs.Position = 4;
                foreach (FileInfo file in dir.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    if (file.Extension == ".assets")
                    {
                        continue;
                    }

                    FileStream ts = file.OpenRead();
                    string rel = Path.GetRelativePath(path, file.FullName);
                    Console.WriteLine($"Packing {filename} <-- {rel}");
                    fs.WriteString(rel);
                    fs.WriteInt64(ts.Length);
                    ts.CopyTo(fs);
                    ts.Close();
                    i++;
                }
                dir.Delete(true);
                fs.Position = 0;
                fs.WriteInt(i);
                fs.Flush();
                fs.Close();
            }
        }
    }
}