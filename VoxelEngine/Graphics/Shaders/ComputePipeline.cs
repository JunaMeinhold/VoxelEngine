namespace VoxelEngine.Rendering.Shaders
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Debugging;

    public class ComputePipeline : IDisposable
    {
        private readonly ID3D11Device device;
        private readonly ComputePipelineDesc desc;
        private ShaderMacro[]? macros;
        private bool isInvalid;

        public readonly ConstantBufferCollection ConstantBuffers = new();
        public readonly ShaderResourceViewCollection ShaderResourceViews = new();
        public readonly SamplerStateCollection SamplerStates = new();
        public readonly UnorderedAccessViewCollection UnorderedAccessViews = new();

        private ID3D11ComputeShader cs;

        private bool disposedValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComputePipeline(ID3D11Device device, ComputePipelineDesc desc)
        {
            this.device = device;
            this.desc = desc;
            Compile();
            Reload += OnReload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComputePipeline(ID3D11Device device, ComputePipelineDesc desc, ShaderMacro[] macros)
        {
            this.device = device;
            this.desc = desc;
            this.macros = macros;
            Compile();
            Reload += OnReload;
        }

        public ComputePipelineDesc Description => desc;

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
            cs?.Dispose();
            Compile(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Compile(bool bypassCache = false)
        {
            isInvalid = false;
            ShaderMacro[] macros = GetShaderMacros();
            if (desc.Shader is not null)
            {
                Shader* shader;
                ShaderCompiler.GetShaderOrCompileFile(desc.ShaderEntry, desc.Shader, "cs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    isInvalid = true;
                    return;
                }

                cs = device.CreateComputeShader(shader->AsSpan());
                cs.DebugName = GetType().Name + nameof(cs);

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
            context.CSSetShader(cs);

            ConstantBuffers.Bind(context);
            ShaderResourceViews.Bind(context);
            SamplerStates.Bind(context);
            UnorderedAccessViews.Bind(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End(ID3D11DeviceContext context)
        {
            context.CSSetShader(null);

            ConstantBuffers.Unbind(context);
            ShaderResourceViews.Unbind(context);
            SamplerStates.Unbind(context);
            UnorderedAccessViews.Unbind(context);
        }

        #endregion Utility

        #region Dispose

        ~ComputePipeline()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Reload -= OnReload;

                cs?.Dispose();
                cs = null;

                ConstantBuffers.DisposeAll();
                ConstantBuffers.Clear();
                ShaderResourceViews.DisposeAll();
                ShaderResourceViews.Clear();
                SamplerStates.DisposeAll();
                SamplerStates.Clear();
                UnorderedAccessViews.DisposeAll();
                UnorderedAccessViews.Clear();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Dispose
    }
}