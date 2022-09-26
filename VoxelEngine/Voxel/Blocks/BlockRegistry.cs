namespace VoxelEngine.Voxel.Blocks
{
    using System.Collections.Generic;

    public static class BlockRegistry
    {
        private static readonly List<BlockEntry> _blocks = new();
        private static readonly List<string> _textures = new();
        private static readonly List<BlockDescription> _descriptions = new();
        private static readonly Dictionary<string, int> blockNameToIndex = new();

        public static IReadOnlyList<BlockEntry> Blocks => _blocks;

        public static IReadOnlyList<string> Textures => _textures;

        public static IReadOnlyList<BlockDescription> Description => _descriptions;

        public static int Count => _blocks.Count + 1;

        public static void Reset()
        {
            _blocks.Clear();
            _textures.Clear();
            _descriptions.Clear();
            blockNameToIndex.Clear();
        }

        public static void RegisterBlock(BlockEntry entry)
        {
            int blockIndex = _blocks.Count;
            entry.Id = (byte)(blockIndex + 1);
            blockNameToIndex.Add(entry.Name, blockIndex);
            _blocks.Add(entry);
            if (entry.Description.IsSingle)
            {
                int textureIndex = _textures.Count;
                _textures.Add(entry.Description.Single);
                _descriptions.Add(new((byte)textureIndex));
            }
            else
            {
                int index = _textures.Count;
                _textures.AddRange(entry.Description.ToArray());
                _descriptions.Add(new((byte)index, (byte)(index + 1), (byte)(index + 2), (byte)(index + 3), (byte)(index + 4), (byte)(index + 5)));
            }
        }

        public static BlockEntry GetBlockByName(string name)
        {
            return _blocks[blockNameToIndex[name]];
        }

        public static string GetBlockNameById(int id)
        {
            return blockNameToIndex.FirstOrDefault(x => x.Value == id).Key;
        }

        public static BlockEntry GetBlockById(int id)
        {
            return _blocks.FirstOrDefault(x => x.Id == id);
        }

        public static IEnumerable<BlockDescriptionPacked> GetDescriptionPackeds()
        {
            foreach (BlockDescription desc in _descriptions)
            {
                yield return desc;
            }
        }
    }
}