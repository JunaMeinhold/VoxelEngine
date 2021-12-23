using HexaEngine.IO;
using System.IO;

namespace HexaEngine.Models.ObjLoader.Loader.Loaders
{
    public class MaterialStreamProvider : IMaterialStreamProvider
    {
        private readonly string basePath = null;

        public MaterialStreamProvider()
        {
        }

        public MaterialStreamProvider(string basePath)
        {
            this.basePath = basePath;
        }

        public Stream Open(string materialFilePath)
        {
            if (basePath is not null)
            {
                return FileSystem.Open(basePath + materialFilePath);
            }
            return File.Open(materialFilePath, FileMode.Open, FileAccess.Read);
        }
    }

    public class MaterialNullStreamProvider : IMaterialStreamProvider
    {
        public Stream Open(string materialFilePath)
        {
            return null;
        }
    }
}