using HexaEngine.Resources;

namespace HexaEngine.Shaders
{
    public struct PixelShaderDescription
    {
        public string Path;

        public string Entry;

        public PixelShaderVersion Version;

        public PixelShaderDescription(string path, string entry, PixelShaderVersion version)
        {
            Path = ResourceManager.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}