namespace VoxelEngine.Rendering.Shaders
{
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using SharpGen.Runtime;
    using Vortice.D3DCompiler;
    using Vortice.Direct3D;
    using Vortice.Direct3D11.Shader;
    using VoxelEngine.IO;

    public static class ShaderCompiler
    {
        public struct ShaderDebugName
        {
            public ushort Flags;       // Reserved, must be set to zero.
            public ushort NameLength;  // Length of the debug name, without null terminator.
                                       // Followed by NameLength bytes of the UTF-8-encoded name.
                                       // Followed by a null terminator.
                                       // Followed by [0-3] zero bytes to align to a 4-byte boundary.
        }

        public class ShaderIncludeHandler : CallbackBase, Include
        {
            public string TargetPath { get; }

            public ShaderIncludeHandler(string targetPath)
            {
                TargetPath = targetPath;
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                string includeFile = GetFilePath(fileName);

                VirtualStream includeStream = FileSystem.Open(includeFile);

                return includeStream;
            }

            private string GetFilePath(string fileName)
            {
                string path = Path.Combine(Path.GetDirectoryName(TargetPath), fileName);
                return path;
            }

            public void Close(Stream stream)
            {
                stream.Dispose();
            }
        }

        public static T Reflect<T>(ReadOnlySpan<byte> blob) where T : ComObject
        {
            return Compiler.Reflect<T>(blob);
        }

        public static void Compile(string shaderPath, string entry, string version, out Blob blob)
        {
            if (Path.GetExtension(shaderPath) == ".cso")
            {
                VirtualStream fs = FileSystem.Open(shaderPath);
                IntPtr ptr = fs.GetIntPtr(out _);
                fs.Dispose();
                blob = new Blob(ptr);
            }
            else
            {
                ShaderFlags flags = (ShaderFlags)(1 << 21);
#if DEBUG && !RELEASE && !SHADER_FORCE_OPTIMIZE
                flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization | ShaderFlags.DebugNameForSource;
#else
                flags |= ShaderFlags.OptimizationLevel2;
#endif
                VirtualStream fs = FileSystem.Open(shaderPath);
                byte[] bytes = fs.GetBytes();
                fs.Dispose();
                ShaderIncludeHandler handler = new(shaderPath);
                Compiler.Compile(bytes, null, handler, entry, Path.GetFullPath(shaderPath), version, flags, out blob, out Blob error);
#if DEBUG && !RELEASE && !SHADER_FORCE_OPTIMIZE
                if (blob != null)
                {
                    Blob pdb = Compiler.GetBlobPart(blob.BufferPointer, blob.BufferSize, ShaderBytecodePart.Pdb, 0);
                    Blob pdbname = Compiler.GetBlobPart(blob.BufferPointer, blob.BufferSize, ShaderBytecodePart.DebugName, 0);
                    ShaderDebugName pDebugNameData = Marshal.PtrToStructure<ShaderDebugName>(pdbname.BufferPointer);
                    string name = Marshal.PtrToStringUTF8(pdbname.BufferPointer + 4, pDebugNameData.NameLength);
                    Trace.WriteLine($"{shaderPath} -> {name}");
                    File.WriteAllBytes(Path.Combine(Paths.CurrentPDBShaderPath, name), pdb.AsBytes().ToArray());
                    pdb.Dispose();
                    pdbname.Dispose();
                    Compiler.StripShader(blob.BufferPointer, blob.BufferSize, StripFlags.CompilerStripDebugInfo, out blob);
                }
#endif
                if (error is not null)
                {
                    string text = Encoding.UTF8.GetString(error.AsBytes());
                    Debug.WriteLine(text);
                }
            }
        }
    }
}