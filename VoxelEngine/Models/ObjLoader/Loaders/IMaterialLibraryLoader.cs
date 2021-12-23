using System.IO;

namespace HexaEngine.Models.ObjLoader.Loader.Loaders
{
    public interface IMaterialLibraryLoader
    {
        void Load(Stream lineStream);
    }
}