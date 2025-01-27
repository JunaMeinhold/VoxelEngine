namespace VoxelEngine.Rendering.DXGI
{
    using System.Diagnostics;
    using Vortice.DXGI;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Rendering.D3D;

    public static class DXGIDeviceManager
    {
        private static IDXGIFactory7 factory;
        private static IDXGIAdapter4 adapter;

        public static event EventHandler OnResize;

        public static void Initialize()
        {
            // Create the DXGIFactory1.
            DXGI.CreateDXGIFactory2(false, out factory);

            // Get the HardwareAdapter.
            adapter = GetHardwareAdapter();

            // Initialize d3d11 Device and DeviceContext.
            D3D11DeviceManager.InitializeDevice(adapter);
        }

        public static SwapChain CreateSwapChain(CoreWindow window)
        {
            var (Hwnd, HDC, HInstance) = window.Win32 ?? throw new NotSupportedException();

            SwapChainDescription1 swapChainDescription = new()
            {
                Width = window.Width,
                Height = window.Height,
                Format = Format.B8G8R8A8_UNorm,
                BufferCount = 3,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = SwapChainFlags.AllowModeSwitch | SwapChainFlags.AllowTearing,
                Stereo = false,
            };

            SwapChainFullscreenDescription fullscreenDescription = new()
            {
                Windowed = true,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            IDXGISwapChain1 swapChain = factory.CreateSwapChainForHwnd(D3D11DeviceManager.ID3D11Device, Hwnd, swapChainDescription, fullscreenDescription);

            return new(D3D11DeviceManager.ID3D11Device, swapChain, swapChainDescription);
        }

        public static void Dispose()
        {
            adapter.Dispose();
            factory.Dispose();
            D3D11DeviceManager.Dispose();
        }

        private static IDXGIAdapter4 GetHardwareAdapter()
        {
            IDXGIAdapter4 selected = null;

            for (int adapterIndex = 0;
                factory.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter) !=
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

                selected = adapter;
            }

            if (selected == null)
                throw new NotSupportedException();
            return selected;
        }
    }
}