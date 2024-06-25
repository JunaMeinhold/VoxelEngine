namespace VoxelEngine.Rendering.D3D.Shaders
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using SharpGen.Runtime;
    using Vortice.D3DCompiler;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Rendering.D3D.Interfaces;

    public class ShaderPipeline<T> : IDisposable where T : IShaderLogic, new()
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ShaderPipeline(ID3D11Device device)
        {
            ShaderLogic = new();
            ShaderLogic.Initialize(device, out Description);
            Initialize(device);
        }

        public readonly T ShaderLogic;
        public readonly ShaderDescription Description;
        private bool isInvalid;

        public readonly List<IConstantBuffer> ConstantBuffers = new();
        public readonly List<IShaderResource> ShaderResources = new();
        private ID3D11VertexShader VertexShader;
        private ID3D11HullShader HullShader;
        private ID3D11DomainShader DomainShader;
        private ID3D11GeometryShader GeometryShader;
        private ID3D11PixelShader PixelShader;
        private ID3D11InputLayout InputLayout;

        private ID3D11RasterizerState RasterizerState;
        private ID3D11DepthStencilState DepthStencilState;
        private ID3D11BlendState BlendState;
        private bool disposedValue;

        public bool IsInvalid => isInvalid;

        #region Pipeline compilation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize(ID3D11Device device)
        {
            if (Description.ConstantBuffers != null)
            {
                ConstantBuffers.AddRange(Description.ConstantBuffers);
            }

            if (Description.ShaderResources != null)
            {
                ShaderResources.AddRange(Description.ShaderResources);
            }

            RasterizerState = device.CreateRasterizerState(Description.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(Description.DepthStencil);
            BlendState = device.CreateBlendState(Description.Blend);

            if (Description.VertexShader.HasValue)
            {
                VertexShaderDescription desc = Description.VertexShader.Value;
                if (ShaderCache.GetShader(desc.Path, out byte[] data, out PointerSize size))
                {
                    VertexShader = device.CreateVertexShader(data);
                    VertexShader.DebugName = GetType().Name + nameof(VertexShader);
                    InputLayout = CreateInputLayout(device, data);
                    InputLayout.DebugName = GetType().Name + nameof(InputLayout);
                }
                else
                {
                    ShaderCompiler.Compile(desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out Blob vBlob);
                    if (vBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }
                    if (!desc.IsPreCompiled)
                    {
                        ShaderCache.CacheShader(desc.Path, vBlob);
                    }

                    VertexShader = device.CreateVertexShader(vBlob);
                    VertexShader.DebugName = GetType().Name + nameof(VertexShader);
                    InputLayout = CreateInputLayout(device, vBlob);
                    InputLayout.DebugName = GetType().Name + nameof(InputLayout);
                    vBlob.Dispose();
                }
            }
            if (Description.HullShader.HasValue)
            {
                HullShaderDescription desc = Description.HullShader.Value;
                if (ShaderCache.GetShader(desc.Path, out byte[] data, out PointerSize size))
                {
                    HullShader = device.CreateHullShader(data);
                    HullShader.DebugName = GetType().Name + nameof(HullShader);
                }
                else
                {
                    ShaderCompiler.Compile(desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }
                    if (!desc.IsPreCompiled)
                    {
                        ShaderCache.CacheShader(desc.Path, pBlob);
                    }

                    HullShader = device.CreateHullShader(pBlob);
                    HullShader.DebugName = GetType().Name + nameof(HullShader);
                    pBlob.Dispose();
                }
            }
            if (Description.DomainShader.HasValue)
            {
                DomainShaderDescription desc = Description.DomainShader.Value;
                if (ShaderCache.GetShader(desc.Path, out byte[] data, out PointerSize size))
                {
                    DomainShader = device.CreateDomainShader(data);
                    DomainShader.DebugName = GetType().Name + nameof(DomainShader);
                }
                else
                {
                    ShaderCompiler.Compile(desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }
                    if (!desc.IsPreCompiled)
                    {
                        ShaderCache.CacheShader(desc.Path, pBlob);
                    }

                    DomainShader = device.CreateDomainShader(pBlob);
                    DomainShader.DebugName = GetType().Name + nameof(DomainShader);
                    pBlob.Dispose();
                }
            }
            if (Description.GeometryShader.HasValue)
            {
                GeometryShaderDescription desc = Description.GeometryShader.Value;
                if (ShaderCache.GetShader(desc.Path, out byte[] data, out PointerSize size))
                {
                    GeometryShader = device.CreateGeometryShader(data);
                    GeometryShader.DebugName = GetType().Name + nameof(GeometryShader);
                }
                else
                {
                    ShaderCompiler.Compile(desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }
                    if (!desc.IsPreCompiled)
                    {
                        ShaderCache.CacheShader(desc.Path, pBlob);
                    }

                    GeometryShader = device.CreateGeometryShader(pBlob);
                    GeometryShader.DebugName = GetType().Name + nameof(GeometryShader);
                    pBlob.Dispose();
                }
            }
            if (Description.PixelShader.HasValue)
            {
                PixelShaderDescription desc = Description.PixelShader.Value;
                if (ShaderCache.GetShader(desc.Path, out byte[] data, out PointerSize size))
                {
                    PixelShader = device.CreatePixelShader(data);
                    PixelShader.DebugName = GetType().Name + nameof(PixelShader);
                }
                else
                {
                    ShaderCompiler.Compile(desc.Path, desc.Entry, desc.Version.ToString().ToLowerInvariant(), out Blob pBlob);
                    if (pBlob == null)
                    {
                        isInvalid = true;
                        return;
                    }
                    if (!desc.IsPreCompiled)
                    {
                        ShaderCache.CacheShader(desc.Path, pBlob);
                    }

                    PixelShader = device.CreatePixelShader(pBlob);
                    PixelShader.DebugName = GetType().Name + nameof(PixelShader);
                    pBlob.Dispose();
                }
            }
            isInvalid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ID3D11InputLayout CreateInputLayout(ID3D11Device device, Blob blob)
        {
            _ = Compiler.GetInputSignatureBlob(blob.BufferPointer, blob.BufferSize, out Blob iblob);
            ID3D11InputLayout layout = device.CreateInputLayout(Description.InputElements, iblob);
            iblob.Dispose();
            return layout;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ID3D11InputLayout CreateInputLayout(ID3D11Device device, byte[] data)
        {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);
            _ = Compiler.GetInputSignatureBlob(ptr, new(data.Length), out Blob iblob);
            ID3D11InputLayout layout = device.CreateInputLayout(Description.InputElements, iblob);
            iblob.Dispose();
            Marshal.FreeHGlobal(ptr);
            return layout;
        }

        #endregion Pipeline compilation

        #region Utility

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BindShaders(ID3D11DeviceContext context)
        {
            if (Description.Topology != PrimitiveTopology.Undefined)
            {
                context.IASetPrimitiveTopology(Description.Topology);
            }

            context.VSSetShader(VertexShader);
            context.HSSetShader(HullShader);
            context.DSSetShader(DomainShader);
            context.GSSetShader(GeometryShader);
            context.PSSetShader(PixelShader);
            context.IASetInputLayout(InputLayout);
            context.RSSetState(RasterizerState);
            context.OMSetBlendState(BlendState);
            context.OMSetDepthStencilState(DepthStencilState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BindBuffers(ID3D11DeviceContext context)
        {
            foreach (IConstantBuffer buffer in ConstantBuffers)
            {
                buffer.Bind(context);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BindResources(ID3D11DeviceContext context)
        {
            foreach (IShaderResource buffer in ShaderResources)
            {
                buffer.Bind(context);
            }
        }

        #endregion Utility

        #region Drawing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(ID3D11DeviceContext context, IView view, Matrix4x4 transform, int vertexCount, int startVertexLocation)
        {
            BindBuffers(context);
            BindShaders(context);
            ShaderLogic.Update(context, view, transform);
            BindResources(context);
            context.Draw(vertexCount, startVertexLocation);
            context.ClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexed(ID3D11DeviceContext context, IView view, Matrix4x4 transform, int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            BindBuffers(context);
            BindShaders(context);
            ShaderLogic.Update(context, view, transform);
            BindResources(context);
            context.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            context.ClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawInstanced(ID3D11DeviceContext context, IView view, Matrix4x4 transform, int vertexCountPerInstance, int instanceCount, int startVertexLocation, int startInstanceLocation)
        {
            BindBuffers(context);
            BindShaders(context);
            ShaderLogic.Update(context, view, transform);
            BindResources(context);
            context.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            context.ClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexedInstanced(ID3D11DeviceContext context, IView view, Matrix4x4 transform, int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
        {
            BindBuffers(context);
            BindShaders(context);
            ShaderLogic.Update(context, view, transform);
            BindResources(context);
            context.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            context.ClearState();
        }

        #endregion Drawing

        #region Dispose

        ~ShaderPipeline()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!disposedValue)
            {
                VertexShader?.Dispose();
                VertexShader = null;
                HullShader?.Dispose();
                HullShader = null;
                DomainShader?.Dispose();
                DomainShader = null;
                PixelShader?.Dispose();
                PixelShader = null;
                InputLayout?.Dispose();
                InputLayout = null;

                RasterizerState?.Dispose();
                RasterizerState = null;
                DepthStencilState?.Dispose();
                DepthStencilState = null;
                BlendState?.Dispose();
                BlendState = null;

                foreach (IConstantBuffer buffer in ConstantBuffers)
                {
                    buffer.Dispose();
                }

                ConstantBuffers.Clear();
                foreach (IShaderResource resource in ShaderResources)
                {
                    resource.Dispose();
                }

                ShaderResources.Clear();

                ShaderLogic.Dispose();

                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion Dispose
    }
}