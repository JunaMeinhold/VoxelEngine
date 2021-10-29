// Code ported from https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/

// Note this implementation does not support different block types or block normals
// The original author describes how to do this here: https://0fps.net/2012/07/07/meshing-minecraft-part-2/
using HexaEngine.Objects;
using HexaEngine.Resources;
using System.Collections.Generic;
using System.Numerics;

namespace HexaEngine.Mathematics
{
    public class BlockVertex
    {
        public List<MeshFace> Faces { get; } = new();

        public void AppendQuad(Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, int w, int h, VoxelFace voxelFace, bool backFace)
        {
            Faces.Add(new MeshFace { BottomLeft = bottomLeft, TopLeft = topLeft, TopRight = topRight, BottomRight = bottomRight, width = w, height = h, voxelFace = voxelFace, backFace = backFace });
        }

        public (Vertex[], int[]) GetDataReduced()
        {
            var vertices = new Vertex[Faces.Count * 4];
            var indices = new int[Faces.Count * 6];
            var indexV = 0;
            var indexI = 0;
            foreach (var meshFace in Faces)
            {
                var plane = VertexPlane.FromMeshFaceReduced(indexI, meshFace);

                if (meshFace.backFace)
                    plane.ReverseOrder();

                plane.Vertices.CopyTo(vertices, indexV);
                indexV += 4;
                plane.Indices.CopyTo(indices, indexI);
                indexI += 6;
            }

            return (vertices, indices);
        }

        public (Vertex[], int[]) GetData()
        {
            var vertices = new Vertex[Faces.Count * 6];
            var indices = new int[Faces.Count * 6];
            var index = 0;
            foreach (var meshFace in Faces)
            {
                var plane = VertexPlane.FromMeshFace(meshFace);

                if (meshFace.backFace)
                    plane.ReverseOrder();

                plane.Vertices.CopyTo(vertices, index);
                index += 6;
            }
            for (int i = 0; i < vertices.Length; i++)
                indices[i] = i;
            return (vertices, indices);
        }
    }
}