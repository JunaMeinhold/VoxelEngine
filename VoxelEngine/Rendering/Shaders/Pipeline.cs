namespace VoxelEngine.Rendering.Shaders
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using SharpGen.Runtime;
    using Vortice.D3DCompiler;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.Direct3D11.Shader;
    using Vortice.DXGI;
    using VoxelEngine.Debugging;
    using VoxelEngine.IO;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using ShaderDescription = Vortice.Direct3D11.Shader.ShaderDescription;

    public struct Binding<T>
    {
        public ShaderStage Stage;
        public int Slot;
        public T Value;

        public Binding(ShaderStage stage, int slot, T value)
        {
            Stage = stage;
            Slot = slot;
            Value = value;
        }
    }

    public abstract class BindingCollection<T> : IList<Binding<T>> where T : IDisposable
    {
        private readonly List<Binding<T>> bindings = new();
        protected T[] vs;
        protected int vsStart;
        protected T[] hs;
        protected int hsStart;
        protected T[] ds;
        protected int dsStart;
        protected T[] gs;
        protected int gsStart;
        protected T[] ps;
        protected int psStart;

        public Binding<T> this[int index] { get => ((IList<Binding<T>>)bindings)[index]; set => ((IList<Binding<T>>)bindings)[index] = value; }

        public int Count => ((ICollection<Binding<T>>)bindings).Count;

        public bool IsReadOnly => ((ICollection<Binding<T>>)bindings).IsReadOnly;

        public abstract void Set(ID3D11DeviceContext context);

        private void UpdateArrays()
        {
            List<KeyValuePair<int, T>> vss = new();
            List<KeyValuePair<int, T>> hss = new();
            List<KeyValuePair<int, T>> dss = new();
            List<KeyValuePair<int, T>> gss = new();
            List<KeyValuePair<int, T>> pss = new();
            foreach (Binding<T> item in bindings)
            {
                switch (item.Stage)
                {
                    case ShaderStage.Vertex:
                        vss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Hull:
                        hss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Domain:
                        dss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Geometry:
                        gss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Pixel:
                        pss.Add(new(item.Slot, item.Value));
                        break;
                }
            }

            vs = ToArray(vss, out vsStart);
            hs = ToArray(hss, out hsStart);
            ds = ToArray(dss, out dsStart);
            gs = ToArray(gss, out gsStart);
            ps = ToArray(pss, out psStart);
        }

        private static T[] ToArray<T>(List<KeyValuePair<int, T>> pairs, out int start)
        {
            if (pairs.Count == 0)
            {
                start = 0;
                return null;
            }

            start = pairs.MinBy(x => x.Key).Key;
            int end = pairs.MaxBy(x => x.Key).Key + 1;
            int length = end - start;
            T[] buffers = new T[length];
            for (int i = 0; i < pairs.Count; i++)
            {
                KeyValuePair<int, T> pair = pairs[i];
                buffers[pair.Key - start] = pair.Value;
            }
            return buffers;
        }

        public void Add(Binding<T> item)
        {
            ((ICollection<Binding<T>>)bindings).Add(item);
            UpdateArrays();
        }

        public void Add(T value, ShaderStage stage, int slot)
        {
            ((ICollection<Binding<T>>)bindings).Add(new(stage, slot, value));
            UpdateArrays();
        }

        public void Append(T value, ShaderStage stage)
        {
            int slot = stage switch
            {
                ShaderStage.Vertex => vsStart + (vs?.Length ?? 0),
                ShaderStage.Hull => hsStart + (hs?.Length ?? 0),
                ShaderStage.Domain => dsStart + (ds?.Length ?? 0),
                ShaderStage.Geometry => gsStart + (gs?.Length ?? 0),
                ShaderStage.Pixel => psStart + (ps?.Length ?? 0),
                _ => throw new InvalidOperationException(),
            };
            ((ICollection<Binding<T>>)bindings).Add(new(stage, slot, value));
            UpdateArrays();
        }

        public void Clear()
        {
            ((ICollection<Binding<T>>)bindings).Clear();
            UpdateArrays();
        }

        public bool Contains(Binding<T> item)
        {
            return ((ICollection<Binding<T>>)bindings).Contains(item);
        }

        public void CopyTo(Binding<T>[] array, int arrayIndex)
        {
            ((ICollection<Binding<T>>)bindings).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Binding<T>> GetEnumerator()
        {
            return ((IEnumerable<Binding<T>>)bindings).GetEnumerator();
        }

        public int IndexOf(Binding<T> item)
        {
            return ((IList<Binding<T>>)bindings).IndexOf(item);
        }

        public void Insert(int index, Binding<T> item)
        {
            ((IList<Binding<T>>)bindings).Insert(index, item);
            UpdateArrays();
        }

        public bool Remove(Binding<T> item)
        {
            bool res = ((ICollection<Binding<T>>)bindings).Remove(item);
            UpdateArrays();
            return res;
        }

        public void RemoveAt(int index)
        {
            ((IList<Binding<T>>)bindings).RemoveAt(index);
            UpdateArrays();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)bindings).GetEnumerator();
        }

        public void DisposeAll()
        {
            foreach (Binding<T> item in bindings)
            {
                item.Value.Dispose();
            }
        }

        public void AddRange(IEnumerable<Binding<T>> values)
        {
            bindings.AddRange(values);
            UpdateArrays();
        }

        public void AddRange(T[] values, ShaderStage stage, int start)
        {
            int len = values.Length;
            Binding<T>[] bindings = new Binding<T>[len];
            for (int i = 0; i < len; i++)
            {
                bindings[i] = new Binding<T>(stage, start + i, values[i]);
            }
            this.bindings.AddRange(bindings);
            UpdateArrays();
        }

        public void AppendRange(T[] values, ShaderStage stage)
        {
            int start = stage switch
            {
                ShaderStage.Vertex => vsStart + (vs?.Length ?? 0),
                ShaderStage.Hull => hsStart + (hs?.Length ?? 0),
                ShaderStage.Domain => dsStart + (ds?.Length ?? 0),
                ShaderStage.Geometry => gsStart + (gs?.Length ?? 0),
                ShaderStage.Pixel => psStart + (ps?.Length ?? 0),
                _ => throw new InvalidOperationException(),
            };
            int len = values.Length;
            Binding<T>[] bindings = new Binding<T>[len];
            for (int i = 0; i < len; i++)
            {
                bindings[i] = new Binding<T>(stage, start + i, values[i]);
            }
            this.bindings.AddRange(bindings);
            UpdateArrays();
        }

        public void RemoveRange(IEnumerable<Binding<T>> values)
        {
            bindings.RemoveAll(x => values.Contains(x));
            UpdateArrays();
        }
    }

    public class ConstantBufferCollection : BindingCollection<ID3D11Buffer>
    {
        public override void Set(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSSetConstantBuffers(vsStart, vs);
            if (hs is not null)
                context.HSSetConstantBuffers(hsStart, hs);
            if (ds is not null)
                context.DSSetConstantBuffers(dsStart, ds);
            if (gs is not null)
                context.GSSetConstantBuffers(gsStart, gs);
            if (ps is not null)
                context.PSSetConstantBuffers(psStart, ps);
        }
    }

    public class ShaderResourceViewCollection : BindingCollection<ID3D11ShaderResourceView>
    {
        public override void Set(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSSetShaderResources(vsStart, vs);
            if (hs is not null)
                context.HSSetShaderResources(hsStart, hs);
            if (ds is not null)
                context.DSSetShaderResources(dsStart, ds);
            if (gs is not null)
                context.GSSetShaderResources(gsStart, gs);
            if (ps is not null)
                context.PSSetShaderResources(psStart, ps);
        }
    }

    public class SamplerStateCollection : BindingCollection<ID3D11SamplerState>
    {
        public override void Set(ID3D11DeviceContext context)
        {
            if (vs is not null)
                context.VSSetSamplers(vsStart, vs);
            if (hs is not null)
                context.HSSetSamplers(hsStart, hs);
            if (ds is not null)
                context.DSSetSamplers(dsStart, ds);
            if (gs is not null)
                context.GSSetSamplers(gsStart, gs);
            if (ps is not null)
                context.PSSetSamplers(psStart, ps);
        }
    }

    public class Pipeline : IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pipeline(ID3D11Device device, PipelineDesc desc)
        {
            this.device = device;
            Description = desc;
            Compile();
            RasterizerState = device.CreateRasterizerState(Description.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(Description.DepthStencil);
            BlendState = device.CreateBlendState(Description.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pipeline(ID3D11Device device, PipelineDesc desc, InputElementDescription[] inputElements)
        {
            this.device = device;
            Description = desc;
            this.inputElements = inputElements;
            Compile();
            RasterizerState = device.CreateRasterizerState(Description.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(Description.DepthStencil);
            BlendState = device.CreateBlendState(Description.Blend);
            Reload += OnReload;
        }

        private readonly ID3D11Device device;
        public readonly PipelineDesc Description;
        private readonly InputElementDescription[] inputElements;
        private bool isInvalid;

        public readonly ConstantBufferCollection ConstantBuffers = new();
        public readonly ShaderResourceViewCollection ShaderResourceViews = new();
        public readonly SamplerStateCollection SamplerStates = new();

        private ID3D11VertexShader vs;
        private ID3D11HullShader hs;
        private ID3D11DomainShader ds;
        private ID3D11GeometryShader gs;
        private ID3D11PixelShader ps;
        private ID3D11InputLayout layout;

        private ID3D11RasterizerState RasterizerState;
        private ID3D11DepthStencilState DepthStencilState;
        private ID3D11BlendState BlendState;
        private bool disposedValue;

        public bool IsInvalid => isInvalid;

        #region Pipeline compilation

        protected virtual ShaderMacro[] GetShaderMacros()
        {
            return Array.Empty<ShaderMacro>();
        }

        public static event EventHandler? Reload;

        public static void ReloadShaders()
        {
            ImGuiConsole.Log(ConsoleMessageType.Info, "recompiling shaders ...");
            Reload?.Invoke(null, EventArgs.Empty);
            ImGuiConsole.Log(ConsoleMessageType.Info, "recompiling shaders ... done!");
        }

        protected virtual void OnReload(object? sender, EventArgs args)
        {
            vs?.Dispose();
            hs?.Dispose();
            ds?.Dispose();
            gs?.Dispose();
            ps?.Dispose();
            layout?.Dispose();
            Compile();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Compile()
        {
            if (Description.VertexShader is not null)
            {
                if (ShaderCache.GetShader(Description.VertexShader, out byte[] data, out _))
                {
                    vs = device.CreateVertexShader(data);
                    vs.DebugName = GetType().Name + nameof(vs);
                    layout = CreateInputLayout(data);
                    layout.DebugName = GetType().Name + nameof(layout);
                }
                else
                {
                    ShaderCompiler.Compile(Paths.CurrentShaderPath + Description.VertexShader, Description.VertexShaderEntrypoint, "vs_5_0", out Blob vBlob);
                    if (vBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }

                    ShaderCache.CacheShader(Description.VertexShader, vBlob);

                    vs = device.CreateVertexShader(vBlob);
                    vs.DebugName = GetType().Name + nameof(vs);
                    layout = CreateInputLayout(vBlob);
                    layout.DebugName = GetType().Name + nameof(layout);
                    vBlob.Dispose();
                }
            }
            if (Description.HullShader is not null)
            {
                if (ShaderCache.GetShader(Description.HullShader, out byte[] data, out _))
                {
                    hs = device.CreateHullShader(data);
                    hs.DebugName = GetType().Name + nameof(hs);
                }
                else
                {
                    ShaderCompiler.Compile(Paths.CurrentShaderPath + Description.HullShader, Description.HullShaderEntrypoint, "hs_5_0", out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }

                    ShaderCache.CacheShader(Description.HullShader, pBlob);

                    hs = device.CreateHullShader(pBlob);
                    hs.DebugName = GetType().Name + nameof(hs);
                    pBlob.Dispose();
                }
            }
            if (Description.DomainShader is not null)
            {
                if (ShaderCache.GetShader(Description.DomainShader, out byte[] data, out PointerSize size))
                {
                    ds = device.CreateDomainShader(data);
                    ds.DebugName = GetType().Name + nameof(ds);
                }
                else
                {
                    ShaderCompiler.Compile(Paths.CurrentShaderPath + Description.DomainShader, Description.DomainShaderEntrypoint, "ds_5_0", out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }

                    ShaderCache.CacheShader(Description.DomainShader, pBlob);

                    ds = device.CreateDomainShader(pBlob);
                    ds.DebugName = GetType().Name + nameof(ds);
                    pBlob.Dispose();
                }
            }
            if (Description.GeometryShader is not null)
            {
                if (ShaderCache.GetShader(Description.GeometryShader, out byte[] data, out PointerSize size))
                {
                    gs = device.CreateGeometryShader(data);
                    gs.DebugName = GetType().Name + nameof(gs);
                }
                else
                {
                    ShaderCompiler.Compile(Paths.CurrentShaderPath + Description.GeometryShader, Description.GeometryShaderEntrypoint, "gs_5_0", out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }

                    ShaderCache.CacheShader(Description.GeometryShader, pBlob);

                    gs = device.CreateGeometryShader(pBlob);
                    gs.DebugName = GetType().Name + nameof(gs);
                    pBlob.Dispose();
                }
            }
            if (Description.PixelShader is not null)
            {
                if (ShaderCache.GetShader(Description.PixelShader, out byte[] data, out PointerSize size))
                {
                    ps = device.CreatePixelShader(data);
                    ps.DebugName = GetType().Name + nameof(ps);
                }
                else
                {
                    ShaderCompiler.Compile(Paths.CurrentShaderPath + Description.PixelShader, Description.PixelShaderEntrypoint, "ps_5_0", out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }

                    ShaderCache.CacheShader(Description.PixelShader, pBlob);

                    ps = device.CreatePixelShader(pBlob);
                    ps.DebugName = GetType().Name + nameof(ps);
                    pBlob.Dispose();
                }
            }
            isInvalid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ID3D11InputLayout CreateInputLayout(Blob blob)
        {
            _ = Compiler.GetInputSignatureBlob(blob.BufferPointer, blob.BufferSize, out Blob iblob);
            ID3D11InputLayout layout = inputElements == null ? CreateInputLayoutFromSignature(blob.AsSpan(), iblob) : device.CreateInputLayout(inputElements, iblob);
            iblob.Dispose();
            return layout;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ID3D11InputLayout CreateInputLayout(byte[] data)
        {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);
            _ = Compiler.GetInputSignatureBlob(ptr, new(data.Length), out Blob iblob);

            ID3D11InputLayout layout = inputElements == null ? CreateInputLayoutFromSignature(data, iblob) : device.CreateInputLayout(inputElements, iblob);

            iblob.Dispose();
            Marshal.FreeHGlobal(ptr);
            return layout;
        }

        private ID3D11InputLayout CreateInputLayoutFromSignature(ReadOnlySpan<byte> shader, Blob signature)
        {
            ID3D11ShaderReflection reflection = ShaderCompiler.Reflect<ID3D11ShaderReflection>(shader);
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

            return device.CreateInputLayout(inputElements, signature);
        }

        #endregion Pipeline compilation

        #region Utility

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginDraw(ID3D11DeviceContext context)
        {
            context.IASetPrimitiveTopology(Description.Topology);
            context.VSSetShader(vs);
            context.HSSetShader(hs);
            context.DSSetShader(ds);
            context.GSSetShader(gs);
            context.PSSetShader(ps);
            context.IASetInputLayout(layout);
            context.RSSetState(RasterizerState);
            context.OMSetBlendState(BlendState);
            context.OMSetDepthStencilState(DepthStencilState);

            ConstantBuffers.Set(context);
            ShaderResourceViews.Set(context);
            SamplerStates.Set(context);
        }

        #endregion Utility

        #region Drawing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(ID3D11DeviceContext context, int vertexCount, int startVertexLocation)
        {
            BeginDraw(context);
            if (vs is not null)
                context.Draw(vertexCount, startVertexLocation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawFast(ID3D11DeviceContext context, int vertexCount, int startVertexLocation)
        {
            context.Draw(vertexCount, startVertexLocation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexed(ID3D11DeviceContext context, int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            BeginDraw(context);
            if (vs is not null)
                context.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawInstanced(ID3D11DeviceContext context, int vertexCountPerInstance, int instanceCount, int startVertexLocation, int startInstanceLocation)
        {
            BeginDraw(context);
            if (vs is not null)
                context.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexedInstanced(ID3D11DeviceContext context, int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
        {
            BeginDraw(context);
            if (vs is not null)
                context.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        #endregion Drawing

        #region Dispose

        ~Pipeline()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Dispose()
        {
            if (!disposedValue)
            {
                Reload -= OnReload;

                vs?.Dispose();
                vs = null;
                hs?.Dispose();
                hs = null;
                ds?.Dispose();
                ds = null;
                gs?.Dispose();
                gs = null;
                ps?.Dispose();
                ps = null;
                layout?.Dispose();
                layout = null;

                RasterizerState?.Dispose();
                RasterizerState = null;
                DepthStencilState?.Dispose();
                DepthStencilState = null;
                BlendState?.Dispose();
                BlendState = null;

                ConstantBuffers.DisposeAll();
                ConstantBuffers.Clear();
                ShaderResourceViews.DisposeAll();
                ShaderResourceViews.Clear();
                SamplerStates.DisposeAll();
                SamplerStates.Clear();

                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion Dispose
    }

    public class Effect<T> where T : Pipeline
    {
        private readonly T pipeline;
        public IRenderTarget Target;
    }
}