﻿namespace VoxelEngine.IO.ObjLoader.Loaders
{
    using System.Collections.Generic;
    using System.IO;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class ObjLoader : LoaderBase, IObjLoader
    {
        private readonly IDataStore _dataStore;
        private readonly List<ITypeParser> _typeParsers = new();

        private readonly List<string> _unrecognizedLines = new();

        public ObjLoader(
            IDataStore dataStore,
            IFaceParser faceParser,
            IGroupParser groupParser,
            INormalParser normalParser,
            ITextureParser textureParser,
            IVertexParser vertexParser,
            IMaterialLibraryParser materialLibraryParser,
            IUseMaterialParser useMaterialParser)
        {
            _dataStore = dataStore;
            SetupTypeParsers(
                vertexParser,
                faceParser,
                normalParser,
                textureParser,
                groupParser,
                materialLibraryParser,
                useMaterialParser);
        }

        private void SetupTypeParsers(params ITypeParser[] parsers)
        {
            foreach (var parser in parsers)
            {
                _typeParsers.Add(parser);
            }
        }

        protected override void ParseLine(string keyword, string data)
        {
            foreach (var typeParser in _typeParsers)
            {
                if (typeParser.CanParse(keyword))
                {
                    typeParser.Parse(data);
                    return;
                }
            }

            _unrecognizedLines.Add(keyword + " " + data);
        }

        public LoadResult Load(Stream lineStream)
        {
            StartLoad(lineStream);

            return CreateResult();
        }

        private LoadResult CreateResult()
        {
            var result = new LoadResult
            {
                Vertices = _dataStore.Vertices,
                Textures = _dataStore.Textures,
                Normals = _dataStore.Normals,
                Groups = _dataStore.Groups,
                Materials = _dataStore.Materials
            };
            return result;
        }
    }
}