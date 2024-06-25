namespace VoxelEngine.Rendering.D3D.Shaders
{
    using VoxelEngine.IO;

    public struct PixelShaderDescription
    {
        public string Path;

        public string Entry;

        public PixelShaderVersion Version;
        public bool IsPreCompiled => System.IO.Path.GetExtension(Path) == ".cso";

        public PixelShaderDescription(string path, string entry, PixelShaderVersion version)
        {
            Path = Paths.CurrentShaderPath + path;
            Entry = entry;
            Version = version;
        }
    }
}