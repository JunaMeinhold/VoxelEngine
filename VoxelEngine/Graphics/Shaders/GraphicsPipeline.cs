namespace VoxelEngine.Rendering.Shaders
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Debugging;

    public class GraphicsPipeline : IDisposable
    {
        private readonly ID3D11Device device;
        private readonly GraphicsPipelineDesc desc;
        private readonly GraphicsPipelineState state;
        private InputElementDescription[]? inputElements;
        private ShaderMacro[]? macros;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc)
        {
            this.device = device;
            this.desc = desc;
            state = GraphicsPipelineState.Default;
            Compile();
            RasterizerState = device.CreateRasterizerState(state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(state.DepthStencil);
            BlendState = device.CreateBlendState(state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, InputElementDescription[] inputElements)
        {
            this.device = device;
            this.desc = desc;
            state = GraphicsPipelineState.Default;
            this.inputElements = inputElements;
            Compile();
            RasterizerState = device.CreateRasterizerState(state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(state.DepthStencil);
            BlendState = device.CreateBlendState(state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, InputElementDescription[] inputElements, ShaderMacro[] macros)
        {
            this.device = device;
            this.desc = desc;
            state = GraphicsPipelineState.Default;
            this.inputElements = inputElements;
            this.macros = macros;
            Compile();
            RasterizerState = device.CreateRasterizerState(state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(state.DepthStencil);
            BlendState = device.CreateBlendState(state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, ShaderMacro[] macros)
        {
            this.device = device;
            this.desc = desc;
            state = GraphicsPipelineState.Default;
            this.macros = macros;
            Compile();
            RasterizerState = device.CreateRasterizerState(state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(state.DepthStencil);
            BlendState = device.CreateBlendState(state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, GraphicsPipelineState state)
        {
            this.device = device;
            this.desc = desc;
            this.state = state;
            Compile();
            RasterizerState = device.CreateRasterizerState(this.state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(this.state.DepthStencil);
            BlendState = device.CreateBlendState(this.state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, GraphicsPipelineState state, InputElementDescription[] inputElements)
        {
            this.device = device;
            this.desc = desc;
            this.state = state;
            this.inputElements = inputElements;
            Compile();
            RasterizerState = device.CreateRasterizerState(this.state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(this.state.DepthStencil);
            BlendState = device.CreateBlendState(this.state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, GraphicsPipelineState state, InputElementDescription[] inputElements, ShaderMacro[] macros)
        {
            this.device = device;
            this.desc = desc;
            this.state = state;
            this.inputElements = inputElements;
            this.macros = macros;
            Compile();
            RasterizerState = device.CreateRasterizerState(this.state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(this.state.DepthStencil);
            BlendState = device.CreateBlendState(this.state.Blend);
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GraphicsPipeline(ID3D11Device device, GraphicsPipelineDesc desc, GraphicsPipelineState state, ShaderMacro[] macros)
        {
            this.device = device;
            this.desc = desc;
            this.state = state;
            this.macros = macros;
            Compile();
            RasterizerState = device.CreateRasterizerState(this.state.Rasterizer);
            DepthStencilState = device.CreateDepthStencilState(this.state.DepthStencil);
            BlendState = device.CreateBlendState(this.state.Blend);
            Reload += OnReload;
        }

        public GraphicsPipelineDesc Description => desc;

        public GraphicsPipelineState State => state;

        public ShaderMacro[]? Macros { get => macros; set => macros = value; }

        public bool IsInvalid => isInvalid;

        #region Pipeline compilation

        protected virtual ShaderMacro[] GetShaderMacros()
        {
            return macros ?? Array.Empty<ShaderMacro>();
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
            Compile(true);
        }

        private static bool CanSkipLayout(InputElementDescription[]? inputElements)
        {
            ArgumentNullException.ThrowIfNull(inputElements, nameof(inputElements));

            for (int i = 0; i < inputElements.Length; i++)
            {
                InputElementDescription inputElement = inputElements[i];
                if (inputElement.SemanticName is not "SV_VertexID" and not "SV_InstanceID")
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Compile(bool bypassCache = false)
        {
            isInvalid = false;
            ShaderMacro[] macros = GetShaderMacros();
            if (desc.VertexShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFileWithInputSignature(desc.VertexShaderEntrypoint, desc.VertexShader, "vs_5_0", macros, &shader, out InputElementDescription[] elements, out Blob signature, bypassCache);
                if (shader == null || signature == null || inputElements == null && elements == null)
                {
                    isInvalid = true;
                    return;
                }

                vs = device.CreateVertexShader(shader->AsSpan());
                vs.DebugName = GetType().Name + nameof(vs);

                inputElements ??= elements;

                if (!CanSkipLayout(inputElements))
                {
                    layout = device.CreateInputLayout(inputElements, signature);
                    layout.DebugName = GetType().Name + nameof(layout);
                }
                else
                {
                    layout = null;
                }

                shader->Release();
                Free(shader);
            }
            if (desc.HullShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.HullShaderEntrypoint, desc.HullShader, "hs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    isInvalid = true;
                    return;
                }

                hs = device.CreateHullShader(shader->AsSpan());
                hs.DebugName = GetType().Name + nameof(hs);

                shader->Release();
                Free(shader);
            }
            if (desc.DomainShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.DomainShaderEntrypoint, desc.DomainShader, "ds_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    isInvalid = true;
                    return;
                }

                ds = device.CreateDomainShader(shader->AsSpan());
                ds.DebugName = GetType().Name + nameof(ds);

                shader->Release();
                Free(shader);
            }
            if (desc.GeometryShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.GeometryShaderEntrypoint, desc.GeometryShader, "gs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    isInvalid = true;
                    return;
                }

                gs = device.CreateGeometryShader(shader->AsSpan());
                gs.DebugName = GetType().Name + nameof(gs);

                shader->Release();
                Free(shader);
            }
            if (desc.PixelShader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.PixelShaderEntrypoint, desc.PixelShader, "ps_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    isInvalid = true;
                    return;
                }

                ps = device.CreatePixelShader(shader->AsSpan());
                ps.DebugName = GetType().Name + nameof(ps);

                shader->Release();
                Free(shader);
            }
            isInvalid = false;
        }

        #endregion Pipeline compilation

        #region Utility

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin(ID3D11DeviceContext context)
        {
            context.IASetPrimitiveTopology(state.Topology);
            context.VSSetShader(vs);
            context.HSSetShader(hs);
            context.DSSetShader(ds);
            context.GSSetShader(gs);
            context.PSSetShader(ps);
            context.IASetInputLayout(layout);
            context.RSSetState(RasterizerState);
            context.OMSetBlendState(BlendState, new Color(state.BlendFactor));
            context.OMSetDepthStencilState(DepthStencilState, (int)state.StencilRef);

            ConstantBuffers.Bind(context);
            ShaderResourceViews.Bind(context);
            SamplerStates.Bind(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End(ID3D11DeviceContext context)
        {
            context.IASetPrimitiveTopology(default);
            context.VSSetShader(null);
            context.HSSetShader(null);
            context.DSSetShader(null);
            context.GSSetShader(null);
            context.PSSetShader(null);
            context.IASetInputLayout(null);
            context.RSSetState(null);
            context.OMSetBlendState(null);
            context.OMSetDepthStencilState(null);

            ConstantBuffers.Unbind(context);
            ShaderResourceViews.Unbind(context);
            SamplerStates.Unbind(context);
        }

        #endregion Utility

        #region Dispose

        ~GraphicsPipeline()
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
}