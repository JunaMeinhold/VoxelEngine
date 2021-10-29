namespace HexaEngine.Mathematics
{
    using HexaEngine.Resources;
    using System.Numerics;

    public struct VertexPlane
    {
        public Vertex[] Vertices;
        public int[] Indices;

        public void ReverseOrder()
        {
            if (Vertices is null) return;
            if (Vertices.Length == 6)
            {
                var tmp = Vertices[0];
                Vertices[0] = Vertices[2];
                Vertices[2] = tmp;
                tmp = Vertices[3];
                Vertices[3] = Vertices[5];
                Vertices[5] = tmp;
            }
            else
            {
                var tmp = Indices[0];
                Indices[0] = Indices[2];
                Indices[2] = tmp;
                tmp = Indices[3];
                Indices[3] = Indices[5];
                Indices[5] = tmp;
            }
        }

        public static VertexPlane From(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight, Vector3 normal, Vector2 texture, int textureIndex, int side)
        {
            if (side == VoxelFace.NORTH | side == VoxelFace.SOUTH)
            {
                var vertices = new Vertex[6];
                vertices[0] = new Vertex(new(topLeft, 1), new Vector3(new Vector2(1, 1) * texture, textureIndex), normal); // new Vector3(new Vector2(0, 0) * texture, textureIndex)
                vertices[1] = new Vertex(new(topRight, 1), new Vector3(new Vector2(1, 0) * texture, textureIndex), normal); // new Vector3(new Vector2(0, 1) * texture, textureIndex)
                vertices[2] = new Vertex(new(bottomLeft, 1), new Vector3(new Vector2(0, 1) * texture, textureIndex), normal); // new Vector3(new Vector2(1, 0) * texture, textureIndex)

                vertices[3] = new Vertex(new(topRight, 1), new Vector3(new Vector2(1, 0) * texture, textureIndex), normal);
                vertices[4] = new Vertex(new(bottomRight, 1), new Vector3(new Vector2(0, 0) * texture, textureIndex), normal); // new Vector3(new Vector2(1, 1) * texture, textureIndex)
                vertices[5] = new Vertex(new(bottomLeft, 1), new Vector3(new Vector2(0, 1) * texture, textureIndex), normal);

                return new() { Vertices = vertices };
            }
            else // Flipped Textures TR and BL
            {
                texture = new Vector2(texture.Y, texture.X);
                var vertices = new Vertex[6];
                vertices[0] = new Vertex(new(topLeft, 1), new Vector3(new Vector2(0, 0) * texture, textureIndex), normal);
                vertices[1] = new Vertex(new(topRight, 1), new Vector3(new Vector2(1, 0) * texture, textureIndex), normal);
                vertices[2] = new Vertex(new(bottomLeft, 1), new Vector3(new Vector2(0, 1) * texture, textureIndex), normal);

                vertices[3] = new Vertex(new(topRight, 1), new Vector3(new Vector2(1, 0) * texture, textureIndex), normal);
                vertices[4] = new Vertex(new(bottomRight, 1), new Vector3(new Vector2(1, 1) * texture, textureIndex), normal);
                vertices[5] = new Vertex(new(bottomLeft, 1), new Vector3(new Vector2(0, 1) * texture, textureIndex), normal);
                return new() { Vertices = vertices };
            }
        }

        public static VertexPlane FromReduced(int offset, Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight, Vector3 normal, Vector2 texture, int textureIndex, int side)
        {
            var vertices = new Vertex[4];
            var indices = new int[6];
            if (side == VoxelFace.NORTH | side == VoxelFace.SOUTH)
            {
                vertices[0] = new Vertex(new(topLeft, 1), new Vector3(new Vector2(1, 1) * texture, textureIndex), normal);
                vertices[1] = new Vertex(new(topRight, 1), new Vector3(new Vector2(1, 0) * texture, textureIndex), normal);
                vertices[2] = new Vertex(new(bottomLeft, 1), new Vector3(new Vector2(0, 1) * texture, textureIndex), normal);
                vertices[3] = new Vertex(new(bottomRight, 1), new Vector3(new Vector2(0, 0) * texture, textureIndex), normal);
                indices[0] = 0 + offset;
                indices[1] = 1 + offset;
                indices[2] = 2 + offset;
                indices[3] = 1 + offset;
                indices[4] = 3 + offset;
                indices[5] = 2 + offset;
            }
            else // Flipped Textures TR and BL
            {
                texture = new Vector2(texture.Y, texture.X);

                vertices[0] = new Vertex(new(topLeft, 1), new Vector3(new Vector2(0, 0) * texture, textureIndex), normal);
                vertices[1] = new Vertex(new(topRight, 1), new Vector3(new Vector2(1, 0) * texture, textureIndex), normal);
                vertices[2] = new Vertex(new(bottomLeft, 1), new Vector3(new Vector2(0, 1) * texture, textureIndex), normal);
                vertices[3] = new Vertex(new(bottomRight, 1), new Vector3(new Vector2(1, 1) * texture, textureIndex), normal);
                indices[0] = 0 + offset; //TL
                indices[1] = 1 + offset; //TR
                indices[2] = 2 + offset; //BL
                indices[3] = 1 + offset; //TR
                indices[4] = 3 + offset; //BR
                indices[5] = 2 + offset; //BL
            }
            return new() { Vertices = vertices, Indices = indices };
        }

        public static VertexPlane FromMeshFace(MeshFace face)
        {
            return From(face.TopLeft, face.TopRight, face.BottomLeft, face.BottomRight, face.GetNormal(), face.GetSize(), face.voxelFace.type.Type[face.voxelFace.side], face.voxelFace.side);
        }

        public static VertexPlane FromMeshFaceReduced(int indexOffset, MeshFace face)
        {
            return FromReduced(indexOffset, face.TopLeft, face.TopRight, face.BottomLeft, face.BottomRight, face.GetNormal(), face.GetSize(), face.voxelFace.type.Type[face.voxelFace.side], face.voxelFace.side);
        }

        public static VertexPlane Front => new()
        {
            Vertices = new Vertex[]
            {
                new(new(-1, -1, -1, 1f), new Vector3(1, 0, 0), new(0, 0, -1)),
                new(new(1, 1, -1, 1f), new Vector3(0, 1, 0), new(0, 0, -1)),
                new(new(1, -1, -1, 1f), new Vector3(0, 0, 0), new(0, 0, -1)),
                new(new(-1, -1, -1, 1f), new Vector3(1, 0, 0), new(0, 0, -1)),
                new(new(-1, 1, -1, 1f), new Vector3(1, 1, 0), new(0, 0, -1)),
                new(new(1, 1, -1, 1f), new Vector3(0, 1, 0), new(0, 0, -1)),
            }
        };

        public static VertexPlane Back => new()
        {
            Vertices = new Vertex[]
            {                new(new(-1, 1, 1, 1f), new Vector3(1, 0, 0), new(0, 0, 1)),
                new(new(1, -1, 1, 1f), new Vector3(0, 1, 0), new(0, 0, 1)),
                new(new(1, 1, 1, 1f), new Vector3(0, 0, 0), new(0, 0, 1)),
                new(new(-1, 1, 1, 1f), new Vector3(1, 0, 0), new(0, 0, 1)),
                new(new(-1, -1, 1, 1f), new Vector3(1, 1, 0), new(0, 0, 1)),
                new(new(1, -1, 1, 1f), new Vector3(0, 1, 0), new(0, 0, 1)),
            }
        };

        public static VertexPlane Up => new()
        {
            Vertices = new Vertex[]
            {
                new(new(-1, 1, -1, 1f), new Vector3(1, 0, 0), new Vector3(0, 1, 0)),
                new(new(1, 1, 1, 1f), new Vector3(0, 1, 0), new Vector3(0, 1, 0)),
                new(new(1, 1, -1, 1f), new Vector3(0, 0, 0), new Vector3(0, 1, 0)),
                new(new(-1, 1, -1, 1f), new Vector3(1, 0, 0), new Vector3(0, 1, 0)),
                new(new(-1, 1, 1, 1f), new Vector3(1, 1, 0), new Vector3(0, 1, 0)),
                new(new(1, 1, 1, 1f), new Vector3(0, 1, 0), new Vector3(0, 1, 0)),
            }
        };

        public static VertexPlane Down => new()
        {
            Vertices = new Vertex[]
            {
                new(new(-1, -1, 1, 1f), new Vector3(0, 0, 0), new(0, -1, 0)),
                new(new(-1, -1, -1, 1f), new Vector3(0, 1, 0), new(0, -1, 0)),
                new(new(1, -1, 1, 1f), new Vector3(1, 0, 0), new(0, -1, 0)),
                new(new(-1, -1, -1, 1f), new Vector3(0, 1, 0), new(0, -1, 0)),
                new(new(1, -1, -1, 1f), new Vector3(1, 1, 0), new(0, -1, 0)),
                new(new(1, -1, 1, 1f), new Vector3(1, 0, 0), new(0, -1, 0)),
            }
        };

        public static VertexPlane Left => new()
        {
            Vertices = new Vertex[]
            {
                new(new(-1, -1, -1, 1f), new Vector3(1, 0, 0), new(-1, 0, 0)),
                new(new(-1, 1, 1, 1f), new Vector3(0, 1, 0), new(-1, 0, 0)),
                new(new(-1, 1, -1, 1f), new Vector3(0, 0, 0), new(-1, 0, 0)),
                new(new(-1, -1, -1, 1f), new Vector3(1, 0, 0), new(-1, 0, 0)),
                new(new(-1, -1, 1, 1f), new Vector3(1, 1, 0), new(-1, 0, 0)),
                new(new(-1, 1, 1, 1f), new Vector3(0, 1, 0), new(-1, 0, 0)),
            }
        };

        public static VertexPlane Right => new()
        {
            Vertices = new Vertex[]
            {
                new(new(1, 1, -1, 1f), new Vector3(1, 0, 0), new Vector3(1, 0, 0)),
                new(new(1, -1, 1, 1f), new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
                new(new(1, -1, -1, 1f), new Vector3(0, 0, 0), new Vector3(1, 0, 0)),
                new(new(1, 1, -1, 1f), new Vector3(1, 0, 0), new Vector3(1, 0, 0)),
                new(new(1, 1, 1, 1f), new Vector3(1, 1, 0), new Vector3(1, 0, 0)),
                new(new(1, -1, 1, 1f), new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
            }
        };
    }
}