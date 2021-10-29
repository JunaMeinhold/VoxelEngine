// Code ported from https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/

// Note this implementation does not support different block types or block normals
// The original author describes how to do this here: https://0fps.net/2012/07/07/meshing-minecraft-part-2/
using System.Diagnostics;
using System.Numerics;

namespace HexaEngine.Mathematics
{
    public struct MeshFace
    {
        // AABB 2D
        public Vector3 BottomLeft;

        public Vector3 TopLeft;
        public Vector3 TopRight;
        public Vector3 BottomRight;

        public int width;
        public int height;
        public VoxelFace voxelFace;
        public bool backFace;

        public Vector3 GetNormal()
        {
            return voxelFace.side switch
            {
                VoxelFace.TOP => new(0, 1, 0),
                VoxelFace.BOTTOM => new(0, -1, 0),
                VoxelFace.NORTH => new(0, 0, 1),
                VoxelFace.EAST => new(1, 0, 0),
                VoxelFace.SOUTH => new(0, 0, -1),
                VoxelFace.WEST => new(-1, 0, 0),
                _ => default,
            };
        }

        public Vector2 GetSize()
        {
            var size = TopRight - BottomLeft;

            return voxelFace.side switch
            {
                VoxelFace.TOP => new(size.Z, size.X),
                VoxelFace.BOTTOM => new(size.Z, size.X),
                VoxelFace.NORTH => new(size.X, size.Y),
                VoxelFace.EAST => new(size.Y, size.Z),
                VoxelFace.SOUTH => new(size.X, size.Y),
                VoxelFace.WEST => new(size.Y, size.Z),
                _ => default,
            };
        }
    }
}