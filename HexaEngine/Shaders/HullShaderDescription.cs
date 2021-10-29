using HexaEngine.Extensions;
using HexaEngine.Resources;

namespace HexaEngine.Shaders
{
    public struct HullShaderDescription
    {
        public string Path;

        public string Entry;

        public HullShaderVersion Version;

        public HullShaderDescription(string path, string entry, HullShaderVersion version)
        {
            Path = ResourceManager.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}