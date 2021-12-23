using HexaEngine.Models.ObjLoader.Loader.Loaders;
using HexaEngine.Models.ObjLoader.Loader.TypeParsers.Interfaces;

namespace HexaEngine.Models.ObjLoader.Loader.TypeParsers
{
    public class MaterialLibraryParser : TypeParserBase, IMaterialLibraryParser
    {
        private readonly IMaterialLibraryLoaderFacade _libraryLoaderFacade;

        public MaterialLibraryParser(IMaterialLibraryLoaderFacade libraryLoaderFacade)
        {
            _libraryLoaderFacade = libraryLoaderFacade;
        }

        protected override string Keyword
        {
            get { return "mtllib"; }
        }

        public override void Parse(string line)
        {
            _libraryLoaderFacade.Load(line);
        }
    }
}