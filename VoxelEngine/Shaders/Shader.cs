using HexaEngine.IO;
using HexaEngine.Resources;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Windows;
using HexaEngine.Windows.Native;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Vortice;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace HexaEngine.Shaders
{
    public abstract class Shader : Resource
    {
#pragma warning disable CA1822 // Member als statisch markieren

        public Shader()
        {
        }

        protected DeviceManager Manager { get; set; } = DeviceManager.Current;

        protected List<InputElementDescription> InputElements { get; } = new();

        protected ID3D11VertexShader VertexShader { get; set; }

        protected ID3D11HullShader HullShader { get; set; }

        protected ID3D11DomainShader DomainShader { get; set; }

        protected ID3D11PixelShader PixelShader { get; set; }

        protected ID3D11InputLayout InputLayout { get; set; }

        protected VertexShaderDescription? VertexShaderDescription { get; set; } = null;

        protected HullShaderDescription? HullShaderDescription { get; set; } = null;

        protected DomainShaderDescription? DomainShaderDescription { get; set; } = null;

        protected PixelShaderDescription? PixelShaderDescription { get; set; } = null;

        protected void Initialize()
        {
            if (VertexShaderDescription.HasValue)
            {
                var desc = VertexShaderDescription.Value;
                if (ShaderCache.GetShader(desc.Path, out var ptr, out var size))
                {
                    VertexShader = Manager.ID3D11Device.CreateVertexShader(ptr, size);
                    VertexShader.DebugName = GetType().Name + nameof(VertexShader);
                    InputLayout = CreateInputLayout(Manager, ptr, size);
                    InputLayout.DebugName = GetType().Name + nameof(InputLayout);
                    ShaderCache.FreePointer(ptr);
                }
                else
                {
                    CompileShader(Manager, desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out var vBlob);
                    ShaderCache.CacheShader(desc.Path, vBlob);
                    VertexShader = Manager.ID3D11Device.CreateVertexShader(vBlob.BufferPointer, vBlob.BufferSize);
                    VertexShader.DebugName = GetType().Name + nameof(VertexShader);
                    InputLayout = CreateInputLayout(Manager, vBlob);
                    InputLayout.DebugName = GetType().Name + nameof(InputLayout);
                    vBlob.Dispose();
                }
            }
            if (HullShaderDescription.HasValue)
            {
                var desc = HullShaderDescription.Value;
                CompileShader(Manager, desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out var pBlob);
                HullShader = Manager.ID3D11Device.CreateHullShader(pBlob.BufferPointer, pBlob.BufferSize);
                HullShader.DebugName = GetType().Name + nameof(HullShader);
                pBlob.Dispose();
            }
            if (DomainShaderDescription.HasValue)
            {
                var desc = DomainShaderDescription.Value;
                CompileShader(Manager, desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out var pBlob);
                DomainShader = Manager.ID3D11Device.CreateDomainShader(pBlob.BufferPointer, pBlob.BufferSize);
                DomainShader.DebugName = GetType().Name + nameof(DomainShader);
                pBlob.Dispose();
            }
            if (PixelShaderDescription.HasValue)
            {
                var desc = PixelShaderDescription.Value;
                if (ShaderCache.GetShader(desc.Path, out var ptr, out var size))
                {
                    PixelShader = Manager.ID3D11Device.CreatePixelShader(ptr, size);
                    PixelShader.DebugName = GetType().Name + nameof(PixelShader);
                    ShaderCache.FreePointer(ptr);
                }
                else
                {
                    CompileShader(Manager, desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out var pBlob);
                    ShaderCache.CacheShader(desc.Path, pBlob);
                    PixelShader = Manager.ID3D11Device.CreatePixelShader(pBlob.BufferPointer, pBlob.BufferSize);
                    PixelShader.DebugName = GetType().Name + nameof(PixelShader);
                    pBlob.Dispose();
                }
            }
        }

        protected void CompileShader(DeviceManager manager, string shaderPath, string entry, string version, out Blob blob)
        {
            var flags = ShaderFlags.None;
#if DEBUG
            flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            var fs = FileSystem.Open(shaderPath);
            var bytes = fs.GetBytes();
            fs.Dispose();

            _ = Compiler.Compile(bytes, entry, Path.GetFileName(shaderPath), version, out blob, out var error);
            //_ = Compiler.CompileFromFile(new FileInfo(shaderPath).FullName, null, null, entry, version, flags, out blob, out var error);
            if (error is not null)
            {
                var text = Encoding.UTF8.GetString(error.GetBytes());
                Debug.WriteLine(text);
                var result = manager.Window.ShowMessageBox($"Shader: {version}",
                    text,
                    MessageBoxButtons.OkCancel,
                    MessageBoxIcon.Warning);
                if (result == DialogResult.Cancel)
                    Application.Exit();
            }
        }

        protected ID3D11InputLayout CreateInputLayout(DeviceManager manager, Blob blob)
        {
            _ = Compiler.GetInputSignatureBlob(blob.BufferPointer, blob.BufferSize, out var iblob);
            var layout = manager.ID3D11Device.CreateInputLayout(InputElements.ToArray(), iblob);
            iblob.Dispose();
            return layout;
        }

        protected ID3D11InputLayout CreateInputLayout(DeviceManager manager, IntPtr ptr, PointerSize size)
        {
            _ = Compiler.GetInputSignatureBlob(ptr, size, out var iblob);
            var layout = manager.ID3D11Device.CreateInputLayout(InputElements.ToArray(), iblob);
            iblob.Dispose();
            return layout;
        }

        protected ID3D11InputLayout CreateInputLayout(DeviceManager manager, ShaderBytecode blob)
        {
            var unmanagedPointer = Marshal.AllocHGlobal(blob.Data.Length);
            Marshal.Copy(blob.Data, 0, unmanagedPointer, blob.Data.Length);
            _ = Compiler.GetInputSignatureBlob(unmanagedPointer, new SharpGen.Runtime.PointerSize(blob.Data.Length), out var iblob);
            var layout = manager.ID3D11Device.CreateInputLayout(InputElements.ToArray(), iblob);
            iblob.Dispose();
            Marshal.FreeHGlobal(unmanagedPointer);
            return layout;
        }

        protected ID3D11Buffer CreateBuffer(BufferDescription description, string name)
        {
            var buffer = Manager.ID3D11Device.CreateBuffer(description);
            buffer.DebugName = GetType().Name + name;
            return buffer;
        }

        protected ID3D11SamplerState CreateSamplerState(SamplerDescription description, string name)
        {
            var state = Manager.ID3D11Device.CreateSamplerState(description);
            state.DebugName = GetType().Name + name;
            return state;
        }

        protected void Write<T>(ID3D11Buffer buffer, T t) where T : unmanaged
        {
            var mapped = Manager.ID3D11DeviceContext.Map(buffer, MapMode.WriteDiscard);
            _ = UnsafeUtilities.Write(mapped.DataPointer, ref t);
            Manager.ID3D11DeviceContext.Unmap(buffer);
        }

        protected void Write<T>(ID3D11Buffer buffer, T[] t) where T : unmanaged
        {
            var mapped = Manager.ID3D11DeviceContext.Map(buffer, MapMode.WriteDiscard);
            _ = UnsafeUtilities.Write(mapped.DataPointer, t);
            Manager.ID3D11DeviceContext.Unmap(buffer);
        }

        public static void SWrite<T>(DeviceManager manager, ID3D11Buffer buffer, T t) where T : unmanaged
        {
            var mapped = manager.ID3D11DeviceContext.Map(buffer, MapMode.WriteDiscard);
            _ = UnsafeUtilities.Write(mapped.DataPointer, ref t);
            manager.ID3D11DeviceContext.Unmap(buffer);
        }

        public static void SWrite<T>(DeviceManager manager, ID3D11Buffer buffer, T[] t) where T : unmanaged
        {
            var mapped = manager.ID3D11DeviceContext.Map(buffer, MapMode.WriteDiscard);
            _ = UnsafeUtilities.Write(mapped.DataPointer, t);
            manager.ID3D11DeviceContext.Unmap(buffer);
        }

#pragma warning restore CA1822 // Member als statisch markieren

        public abstract void Render(IView view, Matrix4x4 transform, int indexCount);

        public virtual void SetParameters(IView view, Matrix4x4 transform, int indexCount)
        {
        }

        protected override void Dispose(bool disposing)
        {
            VertexShader?.Dispose();
            HullShader?.Dispose();
            DomainShader?.Dispose();
            PixelShader?.Dispose();
            InputLayout?.Dispose();
            VertexShader = null;
            HullShader = null;
            DomainShader = null;
            PixelShader = null;
            InputLayout = null;
            base.Dispose(disposing);
        }
    }
}