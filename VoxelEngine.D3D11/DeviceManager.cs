namespace VoxelEngine.Graphics
{
    /*
    public static class GraphicsAdapter
    {
        internal static readonly IDXGIFactory2 Factory;
        internal static readonly IDXGIAdapter1 Adapter;

        static GraphicsAdapter()
        {
            DXGI.CreateDXGIFactory1(out Factory);
            Adapter = GetHardwareAdapter();
        }

        private static IDXGIAdapter1 GetHardwareAdapter()
        {
            IDXGIAdapter1 adapter = null;
            IDXGIFactory6 factory6 = Factory.QueryInterfaceOrNull<IDXGIFactory6>();
            if (factory6 != null)
            {
                for (int adapterIndex = 0;
                    factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out adapter) !=
                    Vortice.DXGI.ResultCode.NotFound;
                    adapterIndex++)
                {
                    Trace.WriteLine($"Found Adapter {adapter.Description1.Description}");
                    AdapterDescription1 desc = adapter.Description1;

                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        // Don't select the Basic Render Driver adapter.
                        adapter.Dispose();
                        continue;
                    }

                    Trace.WriteLine($"Using {adapter.Description1.Description}");

                    return adapter;
                }

                factory6.Dispose();
            }

            if (adapter == null)
                for (int adapterIndex = 0;
                    Factory.EnumAdapters1(adapterIndex, out adapter) != Vortice.DXGI.ResultCode.NotFound;
                    adapterIndex++)
                {
                    AdapterDescription1 desc = adapter.Description1;

                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        // Don't select the Basic Render Driver adapter.
                        adapter.Dispose();
                        continue;
                    }

                    return adapter;
                }

            return adapter;
        }

        public static void CreateGraphics(Window window, out GraphicsDevice device)
        {
            device = new(true, window);
        }

        public static SwapChain CreateSwapChainForWindow(GraphicsDevice device, Window window)
        {
            SwapChainDescription1 swapChainDescription = new()
            {
                Width = window.Width,
                Height = window.Height,
                Format = Format.B8G8R8A8_UNorm,
                BufferCount = 2,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = SwapChainFlags.AllowModeSwitch | SwapChainFlags.AllowTearing,
            };

            SwapChainFullscreenDescription fullscreenDescription = new()
            {
                Windowed = true,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            Factory.MakeWindowAssociation(window.Handle, WindowAssociationFlags.IgnoreAll);

            // Create SwapChain.
            return new(device, Factory.CreateSwapChainForHwnd(D3D11DeviceManager.ID3D11Device, window.Handle, swapChainDescription, fullscreenDescription));
        }
    }

    public class GraphicsDevice
    {
        internal static readonly FeatureLevel[] FeatureLevels =
        {
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
        };

        private readonly FeatureLevel featureLevel;
        private readonly ID3D11Device1 device;
        private readonly ID3D11DeviceContext1 context;
        private readonly ID3D11Debug debug;
        public readonly GraphicsContext Context;
        public readonly SwapChain SwapChain;

        public GraphicsDevice(bool debugMode)
        {
            DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;
            if (debugMode)
                flags |= DeviceCreationFlags.Debug;

            D3D11.D3D11CreateDevice(GraphicsAdapter.Adapter, DriverType.Unknown, flags, FeatureLevels, out ID3D11Device tempDevice, out featureLevel, out ID3D11DeviceContext tempContext);
            device = tempDevice.QueryInterface<ID3D11Device1>();
            device.DebugName = nameof(ID3D11Device);
            context = tempContext.QueryInterface<ID3D11DeviceContext1>();
            context.DebugName = nameof(ID3D11DeviceContext);
            tempContext.Dispose();
            tempDevice.Dispose();

            debug = device.QueryInterface<ID3D11Debug>();

            Context = new(context);
        }

        public GraphicsDevice(bool debugMode, Window window)
        {
            DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;
            if (debugMode)
                flags |= DeviceCreationFlags.Debug;

            D3D11.D3D11CreateDevice(GraphicsAdapter.Adapter, DriverType.Unknown, flags, FeatureLevels, out ID3D11Device tempDevice, out featureLevel, out ID3D11DeviceContext tempContext);
            device = tempDevice.QueryInterface<ID3D11Device1>();
            device.DebugName = nameof(ID3D11Device);
            context = tempContext.QueryInterface<ID3D11DeviceContext1>();
            context.DebugName = nameof(ID3D11DeviceContext);
            tempContext.Dispose();
            tempDevice.Dispose();

            debug = device.QueryInterface<ID3D11Debug>();

            Context = new(context);
            SwapChain = GraphicsAdapter.CreateSwapChainForWindow(this, window);
        }
    }

    public class GraphicsContext
    {
        private readonly ID3D11DeviceContext1 context;

        public GraphicsContext(ID3D11DeviceContext1 context)
        {
            this.context = context;
        }
    }

    public class SwapChain
    {
        private readonly GraphicsDevice device;
        private readonly IDXGISwapChain swapChain;
        private RenderTarget renderTarget;
        private bool disposedValue;

        public SwapChain(GraphicsDevice device, IDXGISwapChain swapChain)
        {
            this.device = device;
            this.swapChain = swapChain;
            InitializeRenderTargets();
        }

        public RenderTarget RenderTarget => renderTarget;

        public DepthStencil DepthStencil { get => renderTarget.DepthStencil; set => renderTarget.DepthStencil = value; }

        private void InitializeRenderTargets()
        {
            ID3D11Texture2D1 buffer = swapChain.GetBuffer<ID3D11Texture2D1>(0);
            renderTarget = new(device, buffer, buffer.Description.Width, buffer.Description.Height);
            buffer.Dispose();
        }

        public void Present(int sync)
        {
            swapChain.Present(sync);
        }

        public void ResizeBuffers(int bufferCount, int width, int height, Format format, SwapChainFlags flags)
        {
            renderTarget.Dispose();
            swapChain.ResizeBuffers(bufferCount, width, height, format, flags);
            InitializeRenderTargets();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                renderTarget?.Dispose();
                swapChain.Dispose();
                disposedValue = true;
            }
        }

        ~SwapChain()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SetTarget(ID3D11DeviceContext context)
        {
            renderTarget.SetTarget(context);
        }

        public void ClearAndSetTarget(ID3D11DeviceContext context)
        {
            renderTarget.ClearAndSetTarget(context);
        }
    }*/
}