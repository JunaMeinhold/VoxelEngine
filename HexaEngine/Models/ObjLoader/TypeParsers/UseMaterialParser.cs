using HexaEngine.Models.ObjLoader.Loader.Data;
using HexaEngine.Models.ObjLoader.Loader.Data.DataStore;
using HexaEngine.Models.ObjLoader.Loader.TypeParsers.Interfaces;

namespace HexaEngine.Models.ObjLoader.Loader.TypeParsers
{
    public class UseMaterialParser : TypeParserBase, IUseMaterialParser
    {
        private readonly IElementGroup _elementGroup;

        public UseMaterialParser(IElementGroup elementGroup)
        {
            _elementGroup = elementGroup;
        }

        protected override string Keyword
        {
            get { return "usemtl"; }
        }

        public override void Parse(string line)
        {
            _elementGroup.SetMaterial(line);
        }
    }
}