namespace VoxelEngine.Rendering.DXGI
{
    using Silk.NET.Core.Native;
    using Silk.NET.DXGI;
    using Silk.NET.SDL;

    using VoxelEngine.Core;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Graphics;
    using VoxelEngine.Rendering.D3D;

    public static unsafe class DXGIDeviceManager
    {
        public static readonly DXGI DXGI = DXGI.GetApi();
        private static ComPtr<IDXGIFactory7> factory;
        private static ComPtr<IDXGIAdapter4> adapter;

        public static event EventHandler OnResize;

        public static void Initialize()
        {
            // Create the DXGIFactory1.
            DXGI.CreateDXGIFactory2(0, out factory);

            // Get the HardwareAdapter.
            adapter = GetHardwareAdapter();

            // Initialize d3d11 Device and DeviceContext.
            D3D11DeviceManager.InitializeDevice(adapter);
        }

        public static DXGISwapChain CreateSwapChain(SdlWindow window)
        {
            var (Hwnd, HDC, HInstance) = window.Win32 ?? throw new NotSupportedException();

            SwapChainDesc1 swapChainDescription = new()
            {
                Width = (uint)window.Width,
                Height = (uint)window.Height,
                Format = Format.FormatB8G8R8A8Unorm,
                BufferCount = 2,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SampleDesc = new SampleDesc(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = (uint)(SwapChainFlag.AllowModeSwitch | SwapChainFlag.AllowTearing),
                Stereo = false,
            };

            SwapChainFullscreenDesc fullscreenDescription = new()
            {
                Windowed = true,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            IDXGISwapChain1* swapChain;
            factory.CreateSwapChainForHwnd((IUnknown*)D3D11DeviceManager.ID3D11Device.Handle, Hwnd, &swapChainDescription, &fullscreenDescription, (IDXGIOutput*)null, &swapChain).ThrowHResult();

            return new(D3D11DeviceManager.ID3D11Device, swapChain, swapChainDescription);
        }

        public static unsafe DXGISwapChain CreateSwapChain(Window* window)
        {
            int width, height;
            Application.sdl.GetWindowSize(window, &width, &height);

            SysWMInfo wmInfo;
            Application.sdl.GetVersion(&wmInfo.Version);
            Application.sdl.GetWindowWMInfo(window, &wmInfo);

            SwapChainDesc1 swapChainDescription = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = Format.FormatB8G8R8A8Unorm,
                BufferCount = 2,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SampleDesc = new SampleDesc(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = (uint)(SwapChainFlag.AllowModeSwitch | SwapChainFlag.AllowTearing),
            };

            SwapChainFullscreenDesc fullscreenDescription = new()
            {
                Windowed = true,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            factory.CreateSwapChainForHwnd(D3D11DeviceManager.ID3D11Device, wmInfo.Info.Win.Hwnd, &swapChainDescription, &fullscreenDescription, (IDXGIOutput*)null, out ComPtr<IDXGISwapChain1> swapChain);

            // Create SwapChain.
            return new(D3D11DeviceManager.ID3D11Device, swapChain);
        }

        public static void Dispose()
        {
            adapter.Dispose();
            factory.Dispose();
            D3D11DeviceManager.Dispose();
        }

        private static ComPtr<IDXGIAdapter4> GetHardwareAdapter()
        {
            ComPtr<IDXGIAdapter4> selected = null;

            for (uint adapterIndex = 0;
                factory.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out ComPtr<IDXGIAdapter4> adapter) !=
                (int)ResultCode.DXGI_ERROR_NOT_FOUND;
                adapterIndex++)
            {
                AdapterDesc1 desc;
                adapter.GetDesc1(&desc);

                if (((AdapterFlag)desc.Flags & AdapterFlag.Software) != 0)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Dispose();
                    continue;
                }

                //Trace.WriteLine($"Using {adapter.Description1.Description}");

                selected = adapter;
            }

            if (selected.Handle == null)
            {
                throw new NotSupportedException();
            }

            return selected;
        }
    }
}