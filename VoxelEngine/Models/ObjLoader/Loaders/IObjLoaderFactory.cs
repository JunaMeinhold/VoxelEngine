namespace HexaEngine.Models.ObjLoader.Loader.Loaders
{
    public interface IObjLoaderFactory
    {
        IObjLoader Create(IMaterialStreamProvider materialStreamProvider);
        IObjLoader Create();
    }
}