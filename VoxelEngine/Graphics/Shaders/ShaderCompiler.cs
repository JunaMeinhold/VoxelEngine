namespace VoxelEngine.Rendering.Shaders
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using SharpGen.Runtime;
    using Vortice.D3DCompiler;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.Direct3D11.Shader;
    using Vortice.DXGI;
    using VoxelEngine.Debugging;
    using VoxelEngine.IO;

    public static unsafe class ShaderCompiler
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

        public static void Compile(string source, ShaderMacro[] macros, string entryPoint, string sourceName, string profile, out Blob? shaderBlob, out string? error)
        {
            shaderBlob = null;
            error = null;
            macros = macros.Append(new ShaderMacro(null, null)).ToArray();

            ShaderFlags flags = (ShaderFlags)(1 << 21);
#if DEBUG && !RELEASE && !SHADER_FORCE_OPTIMIZE
                flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization | ShaderFlags.DebugNameForSource;
#else
            flags |= ShaderFlags.OptimizationLevel2;
#endif

            byte[] bytes = Encoding.UTF8.GetBytes(source);
            ShaderIncludeHandler handler = new(Paths.CurrentShaderPath + sourceName);
            Compiler.Compile(bytes, macros, handler, entryPoint, Path.GetFullPath(sourceName), profile, flags, out shaderBlob, out Blob errorBlob);
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

            if (errorBlob != null)
            {
                error = ToStringFromUTF8((byte*)errorBlob.BufferPointer);
                errorBlob.Release();
            }
        }

        public static unsafe Blob GetInputSignature(Blob shader)
        {
            Blob output = Compiler.GetInputSignatureBlob(new Span<byte>((void*)shader.BufferPointer, (int)shader.BufferSize));
            return output;
        }

        public static unsafe Blob GetInputSignature(Shader* shader)
        {
            Blob output = Compiler.GetInputSignatureBlob(new Span<byte>(shader->Bytecode, (int)shader->Length));
            return output;
        }

        public static unsafe void Reflect<T>(Shader* blob, out T reflector) where T : ComObject
        {
            Compiler.Reflect(new Span<byte>(blob->Bytecode, (int)blob->Length), out reflector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Compile(string code, ShaderMacro[] macros, string entry, string sourceName, string profile, Shader** shader, out string? error)
        {
            Compile(code, macros, entry, sourceName, profile, out var shaderBlob, out error);
            if (shaderBlob != null)
            {
                Shader* pShader = AllocT<Shader>();
                pShader->Bytecode = AllocCopy((byte*)shaderBlob.BufferPointer, shaderBlob.BufferSize);
                pShader->Length = (nuint)(int)shaderBlob.BufferSize;
                *shader = pShader;
            }

            if (error != null)
            {
                ImGuiConsole.Log(error);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Compile(string code, string entry, string sourceName, string profile, Shader** shader, out string? error)
        {
            Compile(code, Array.Empty<ShaderMacro>(), entry, sourceName, profile, shader, out error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Compile(string code, ShaderMacro[] macros, string entry, string sourceName, string profile, Shader** shader)
        {
            Compile(code, macros, entry, sourceName, profile, shader, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Compile(string code, string entry, string sourceName, string profile, Shader** shader)
        {
            Compile(code, entry, sourceName, profile, shader, out _);
        }

        public static unsafe void GetShaderOrCompileFile(string entry, string path, string profile, ShaderMacro[] macros, Shader** shader, bool bypassCache = false)
        {
            string text = FileSystem.ReadAllText(Paths.CurrentShaderPath + path);
            uint hash = CRC32.Crc32(text);
            Shader* pShader;
            if (bypassCache || !ShaderCache.GetShader(path, SourceLanguage.HLSL, hash, macros, &pShader, out _))
            {
                Compile(text, macros, entry, path, profile, &pShader);
                if (pShader == null)
                {
                    return;
                }

                ShaderCache.CacheShader(path, SourceLanguage.HLSL, hash, macros, Array.Empty<InputElementDescription>(), pShader);
            }
            *shader = pShader;
        }

        public static unsafe void GetShaderOrCompileFileWithInputSignature(string entry, string path, string profile, ShaderMacro[] macros, Shader** shader, out InputElementDescription[]? inputElements, out Blob? signature, bool bypassCache = false)
        {
            string text = FileSystem.ReadAllText(Paths.CurrentShaderPath + path);
            uint hash = CRC32.Crc32(text);
            Shader* pShader;
            if (bypassCache || !ShaderCache.GetShader(path, SourceLanguage.HLSL, hash, macros, &pShader, out inputElements))
            {
                Compile(text, macros, entry, path, profile, &pShader);
                signature = null;
                inputElements = null;
                if (pShader == null)
                {
                    return;
                }

                signature = GetInputSignature(pShader);
                inputElements = GetInputElementsFromSignature(pShader, signature);
                ShaderCache.CacheShader(path, SourceLanguage.HLSL, hash, macros, inputElements, pShader);
            }
            *shader = pShader;
            signature = GetInputSignature(pShader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe InputElementDescription[] GetInputElementsFromSignature(Shader* shader, Blob signature)
        {
            ID3D11ShaderReflection reflection = ShaderCompiler.Reflect<ID3D11ShaderReflection>(shader->AsSpan());
            ShaderDescription desc = reflection.Description;

            InputElementDescription[] inputElements = new InputElementDescription[desc.InputParameters];
            for (uint i = 0; i < desc.InputParameters; i++)
            {
                ShaderParameterDescription parameterDesc = reflection.InputParameters[i];

                InputElementDescription inputElement = new()
                {
                    Slot = 0,
                    SemanticName = parameterDesc.SemanticName,
                    SemanticIndex = parameterDesc.SemanticIndex,
                    Classification = InputClassification.PerVertexData,
                    AlignedByteOffset = InputElementDescription.AppendAligned,
                    InstanceDataStepRate = 0
                };

                if (parameterDesc.UsageMask == RegisterComponentMaskFlags.ComponentX)
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.UInt32 => Format.R32_UInt,
                        RegisterComponentType.SInt32 => Format.R32_SInt,
                        RegisterComponentType.Float32 => Format.R32_Float,
                        _ => Format.Unknown,
                    };
                }

                if (parameterDesc.UsageMask == (RegisterComponentMaskFlags.ComponentX | RegisterComponentMaskFlags.ComponentY))
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.UInt32 => Format.R32G32_UInt,
                        RegisterComponentType.SInt32 => Format.R32G32_SInt,
                        RegisterComponentType.Float32 => Format.R32G32_Float,
                        _ => Format.Unknown,
                    };
                }

                if (parameterDesc.UsageMask == (RegisterComponentMaskFlags.ComponentX | RegisterComponentMaskFlags.ComponentY | RegisterComponentMaskFlags.ComponentZ))
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.UInt32 => Format.R32G32B32_UInt,
                        RegisterComponentType.SInt32 => Format.R32G32B32_SInt,
                        RegisterComponentType.Float32 => Format.R32G32B32_Float,
                        _ => Format.Unknown,
                    };
                }

                if (parameterDesc.UsageMask == (RegisterComponentMaskFlags.ComponentX | RegisterComponentMaskFlags.ComponentY | RegisterComponentMaskFlags.ComponentZ | RegisterComponentMaskFlags.ComponentW))
                {
                    inputElement.Format = parameterDesc.ComponentType switch
                    {
                        RegisterComponentType.UInt32 => Format.R32G32B32A32_UInt,
                        RegisterComponentType.SInt32 => Format.R32G32B32A32_SInt,
                        RegisterComponentType.Float32 => Format.R32G32B32A32_Float,
                        _ => Format.Unknown,
                    };
                }

                inputElements[i] = inputElement;
            }

            reflection.Release();
            return inputElements;
        }
    }
}