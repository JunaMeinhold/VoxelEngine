using HexaEngine.Extensions;
using HexaEngine.Resources;

namespace HexaEngine.Shaders
{
    public struct DomainShaderDescription
    {
        public string Path;

        public string Entry;

        public DomainShaderVersion Version;

        public DomainShaderDescription(string path, string entry, DomainShaderVersion version)
        {
            Path = ResourceManager.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}