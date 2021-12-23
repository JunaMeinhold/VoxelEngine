using HexaEngine.Resources;

namespace HexaEngine.Shaders
{
    public struct VertexShaderDescription
    {
        public string Path;

        public string Entry;

        public VertexShaderVersion Version;

        public VertexShaderDescription(string path, string entry, VertexShaderVersion version)
        {
            Path = ResourceManager.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}