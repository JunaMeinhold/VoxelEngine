﻿namespace VoxelEngine.IO.ObjLoader.TypeParsers
{
    using System;
    using VoxelEngine.IO.ObjLoader.Common;
    using VoxelEngine.IO.ObjLoader.Data.DataStore;
    using VoxelEngine.IO.ObjLoader.Data.Elements;
    using VoxelEngine.IO.ObjLoader.TypeParsers.Interfaces;

    public class FaceParser : TypeParserBase, IFaceParser
    {
        private readonly IFaceGroup _faceGroup;

        public FaceParser(IFaceGroup faceGroup)
        {
            _faceGroup = faceGroup;
        }

        protected override string Keyword => "f";

        public override void Parse(string line)
        {
            var vertices = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var face = new Face();

            foreach (var vertexString in vertices)
            {
                var faceVertex = ParseFaceVertex(vertexString);
                face.AddVertex(faceVertex);
            }

            _faceGroup.AddFace(face);
        }

        private static FaceVertex ParseFaceVertex(string vertexString)
        {
            var fields = vertexString.Split(new[] { '/' }, StringSplitOptions.None);

            var vertexIndex = fields[0].ParseInvariantInt();
            var faceVertex = new FaceVertex(vertexIndex, 0, 0);

            if (fields.Length > 1)
            {
                var textureIndex = fields[1].Length == 0 ? 0 : fields[1].ParseInvariantInt();
                faceVertex.TextureIndex = textureIndex;
            }

            if (fields.Length > 2)
            {
                var normalIndex = fields.Length > 2 && fields[2].Length == 0 ? 0 : fields[2].ParseInvariantInt();
                faceVertex.NormalIndex = normalIndex;
            }

            return faceVertex;
        }
    }
}