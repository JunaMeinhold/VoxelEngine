namespace VoxelEngine.Rendering.DXGI
{
    using System.Diagnostics;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using VoxelEngine.Core;
    using VoxelEngine.Rendering.D3D;

    public static class DXGIDeviceManager
    {
        private static IDXGIFactory2 _idxgiFactory;

        public static Window Window { get; private set; }

        public static int Width { get; private set; }

        public static int Height { get; private set; }

        public static float AspectRatio { get; private set; }

        public static IDXGIFactory2 IDXGIFactory => _idxgiFactory;

        public static IDXGIAdapter1 IDXGIAdapter { get; private set; }

        public static SwapChain SwapChain { get; private set; }

        public static Viewport Viewport => new(0, 0, Width, Height, Nucleus.Settings.MinDepth, Nucleus.Settings.MaxDepth);

        public static event EventHandler OnResize;

        public static void Initialize(Window surface)
        {
            Window = surface;
            Width = surface.Width;
            Height = surface.Height;
            AspectRatio = (float)Width / Height;

            // Create the DXGIFactory1.
            DXGI.CreateDXGIFactory1(out _idxgiFactory);

            // Get the HardwareAdapter.
            IDXGIAdapter = GetHardwareAdapter();

            // Initialize d3d11 Device and DeviceContext.
            D3D11DeviceManager.InitializeDevice(IDXGIAdapter);

            SwapChainDescription1 swapChainDescription = new()
            {
                Width = surface.Width,
                Height = surface.Height,
                Format = Format.B8G8R8A8_UNorm,
                BufferCount = Nucleus.Settings.BufferCount,
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

            // Create SwapChain.
            SwapChain = new(D3D11DeviceManager.ID3D11Device, IDXGIFactory.CreateSwapChainForHwnd(D3D11DeviceManager.ID3D11Device, surface.GetHWND(), swapChainDescription, fullscreenDescription));

            // Make window association.
            IDXGIFactory.MakeWindowAssociation(surface.GetHWND(), WindowAssociationFlags.IgnoreAll);

            // Get DXGIDevice.

            D3D11DeviceManager.Initialize();
        }

        public static void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            AspectRatio = (float)Width / Height;

            D3D11DeviceManager.ResizeBegin();

            // Resize SwapChainBuffers.
            SwapChain.ResizeBuffers(Nucleus.Settings.BufferCount, Width, Height, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowModeSwitch | SwapChainFlags.AllowTearing);

            D3D11DeviceManager.ResizeEnd();

            // Notify size change.
            OnResize?.Invoke(null, null);
        }

        public static void Dispose()
        {
            SwapChain.Dispose();
            IDXGIAdapter.Dispose();
            IDXGIFactory.Dispose();
            D3D11DeviceManager.Dispose();
        }

        private static IDXGIAdapter1 GetHardwareAdapter()
        {
            IDXGIAdapter1? adapter = null;
            IDXGIFactory6? factory6 = IDXGIFactory.QueryInterfaceOrNull<IDXGIFactory6>();
            if (factory6 != null)
            {
                for (int adapterIndex = 0;
                    factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out adapter) !=
                    ResultCode.NotFound;
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
            {
                for (int adapterIndex = 0;
                    IDXGIFactory.EnumAdapters1(adapterIndex, out adapter) != ResultCode.NotFound;
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
            }

            return adapter;
        }
    }
}