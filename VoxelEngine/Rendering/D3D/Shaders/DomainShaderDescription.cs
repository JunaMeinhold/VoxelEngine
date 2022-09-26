namespace VoxelEngine.Rendering.D3D.Shaders
{
    using VoxelEngine.IO;

    public struct DomainShaderDescription
    {
        public string Path;

        public string Entry;

        public DomainShaderVersion Version;
        public bool IsPreCompiled => System.IO.Path.GetExtension(Path) == ".cso";

        public DomainShaderDescription(string path, string entry, DomainShaderVersion version)
        {
            Path = Paths.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}