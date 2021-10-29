namespace HexaEngine.Objects
{
    using System.Collections.Generic;
    using VoxelGen;

    public static class Registry
    {
        public static List<string> Textures { get; } = new();

        public static void RegisterBlock(params string[] texture)
        {
            Textures.AddRange(texture);
        }

        public static class Blocks
        {
            public static Block Stone = new Block()
            {
                index = 2,
                kind = 1,
                health = 15,
            };

            public static Block Dirt = new Block()
            {
                index = 1,
                kind = 1,
                health = 15,
            };

            public static Block Grass = new Block()
            {
                index = 3,
                kind = 1,
                health = 15,
            };
        }
    }
}