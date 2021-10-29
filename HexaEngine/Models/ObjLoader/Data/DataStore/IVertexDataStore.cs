using HexaEngine.Models.ObjLoader.Loader.Data.VertexData;

namespace HexaEngine.Models.ObjLoader.Loader.Data.DataStore
{
    public interface IVertexDataStore
    {
        void AddVertex(Vertex vertex);
    }
}