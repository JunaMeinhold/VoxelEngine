namespace VoxelEngine.Rendering.D3D.Shaders
{
    using VoxelEngine.IO;

    public struct VertexShaderDescription
    {
        public string Path;

        public string Entry;

        public VertexShaderVersion Version;
        public bool IsPreCompiled => System.IO.Path.GetExtension(Path) == ".cso";

        public VertexShaderDescription(string path, string entry, VertexShaderVersion version)
        {
            Path = Paths.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }

    public struct GeometryShaderDescription
    {
        public string Path;

        public string Entry;

        public GeometryShaderVersion Version;
        public bool IsPreCompiled => System.IO.Path.GetExtension(Path) == ".cso";

        public GeometryShaderDescription(string path, string entry, GeometryShaderVersion version)
        {
            Path = Paths.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}