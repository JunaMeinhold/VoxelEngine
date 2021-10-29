using HexaEngine.Models.ObjLoader.Loader.Loaders;
using HexaEngine.Resources;
using System.Collections.Generic;
using System.IO;

namespace HexaEngine.Models.ObjLoader
{
    public class ModelConverter
    {
        public static void Convert(string path)
        {
            var file = new FileInfo(path);
            var before = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(file.DirectoryName);

            using var fs = file.OpenRead();
            ObjLoaderFactory factory = new();
            var loader = factory.Create();
            var result = loader.Load(fs);
            List<Vertex> vertices = new();
            for (int i = 0; i < result.Groups.Count; i++)
            {
                for (int j = 0; i < result.Groups[i].Faces.Count; j++)
                {
                    for (int jj = 0; i < result.Groups[i].Faces[i].Count; jj++)
                    {
                    }
                }
            }
        }
    }
}