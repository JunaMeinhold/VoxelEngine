namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System.IO;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.TypeParsers;

    public interface IMaterialStreamProvider
    {
        Stream Open(string materialFilePath);
    }

    public class ObjLoaderFactory : IObjLoaderFactory
    {
        public IObjLoader Create()
        {
            return Create(new MaterialStreamProvider());
        }

        public IObjLoader Create(IMaterialStreamProvider materialStreamProvider)
        {
            var dataStore = new DataStore();

            var faceParser = new FaceParser(dataStore);
            var groupParser = new GroupParser(dataStore);
            var normalParser = new NormalParser(dataStore);
            var textureParser = new TextureParser(dataStore);
            var vertexParser = new VertexParser(dataStore);

            var materialLibraryLoader = new MaterialLibraryLoader(dataStore);
            var materialLibraryLoaderFacade = new MaterialLibraryLoaderFacade(materialLibraryLoader, materialStreamProvider);
            var materialLibraryParser = new MaterialLibraryParser(materialLibraryLoaderFacade);
            var useMaterialParser = new UseMaterialParser(dataStore);

            return new ObjLoader(dataStore, faceParser, groupParser, normalParser, textureParser, vertexParser, materialLibraryParser, useMaterialParser);
        }
    }
}