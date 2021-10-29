namespace HexaEngine.Resources
{
    using System.Collections.Generic;
    using Vortice.Direct3D11;

    public class TextureList
    {
        private readonly List<string> paths = new();
        private Texture Texture;
        private bool isLoaded;

        public bool IsLoaded => isLoaded;

        public void Render(ID3D11DeviceContext context)
        {
            Texture?.Render(context);
        }

        public int Add(string path)
        {
            var index = paths.Count;
            paths.Add(path);

            return index;
        }

        public void AddRange(IEnumerable<string> path)
        {
            paths.AddRange(path);
        }

        public void Load()
        {
            if (isLoaded) return;
            Texture = ResourceManager.LoadTexture(paths.ToArray());
            isLoaded = true;
        }

        public void Unload()
        {
            Texture.Dispose();
        }
    }
}