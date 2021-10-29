using System.IO;

namespace HexaEngine.Models.ObjLoader.Loader.Loaders
{
    public interface IObjLoader
    {
        LoadResult Load(Stream lineStream);
    }
}