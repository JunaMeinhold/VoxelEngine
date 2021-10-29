using System.Collections.Generic;
using HexaEngine.Models.ObjLoader.Loader.Data.Elements;
using HexaEngine.Models.ObjLoader.Loader.Data.VertexData;

namespace HexaEngine.Models.ObjLoader.Loader.Data.DataStore
{
    public interface IDataStore
    {
        IList<Vertex> Vertices { get; }
        IList<Texture> Textures { get; }
        IList<Normal> Normals { get; }
        IList<Material> Materials { get; }
        IList<Group> Groups { get; }
    }
}