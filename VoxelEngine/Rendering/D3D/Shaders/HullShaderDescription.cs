namespace VoxelEngine.Rendering.D3D.Shaders
{
    using VoxelEngine.IO;

    public struct HullShaderDescription
    {
        public string Path;

        public string Entry;

        public HullShaderVersion Version;

        public bool IsPreCompiled => System.IO.Path.GetExtension(Path) == ".cso";

        public HullShaderDescription(string path, string entry, HullShaderVersion version)
        {
            Path = Paths.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}