using HexaEngine.Models.ObjLoader.Loader.Common;
using HexaEngine.Models.ObjLoader.Loader.Data;
using HexaEngine.Models.ObjLoader.Loader.Data.DataStore;
using HexaEngine.Models.ObjLoader.Loader.Data.VertexData;
using HexaEngine.Models.ObjLoader.Loader.TypeParsers.Interfaces;

namespace HexaEngine.Models.ObjLoader.Loader.TypeParsers
{
    public class TextureParser : TypeParserBase, ITextureParser
    {
        private readonly ITextureDataStore _textureDataStore;

        public TextureParser(ITextureDataStore textureDataStore)
        {
            _textureDataStore = textureDataStore;
        }

        protected override string Keyword
        {
            get { return "vt"; }
        }

        public override void Parse(string line)
        {
            string[] parts = line.Split(' ');

            float x = parts[0].ParseInvariantFloat();
            float y = parts[1].ParseInvariantFloat();

            var texture = new Texture(x, y);
            _textureDataStore.AddTexture(texture);
        }
    }
}