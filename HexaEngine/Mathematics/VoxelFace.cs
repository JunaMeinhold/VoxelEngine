// Code ported from https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/

// Note this implementation does not support different block types or block normals
// The original author describes how to do this here: https://0fps.net/2012/07/07/meshing-minecraft-part-2/

using HexaEngine.Objects;
using VoxelGen;

namespace HexaEngine.Mathematics
{
    public class VoxelFace
    {
        public bool transparent;
        public Block type;
        public int side;

        public const int SOUTH = 0;
        public const int NORTH = 1;
        public const int EAST = 2;
        public const int WEST = 3;
        public const int TOP = 4;
        public const int BOTTOM = 5;

        public bool Equals(VoxelFace face)
        {
            return face.transparent == transparent && face.type.Kind == type.Kind;
        }
    }
}