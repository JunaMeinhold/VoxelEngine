using System.Collections.Generic;
using HexaEngine.Models.ObjLoader.Loader.Data;
using HexaEngine.Models.ObjLoader.Loader.Data.Elements;
using HexaEngine.Models.ObjLoader.Loader.Data.VertexData;

namespace HexaEngine.Models.ObjLoader.Loader.Loaders
{
    public class LoadResult
    {
        public IList<Vertex> Vertices { get; set; }
        public IList<Texture> Textures { get; set; }
        public IList<Normal> Normals { get; set; }
        public IList<Group> Groups { get; set; }
        public IList<Material> Materials { get; set; }
    }
}