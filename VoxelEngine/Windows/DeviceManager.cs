using HexaEngine.Audio;
using HexaEngine.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using FillMode = Vortice.Direct3D11.FillMode;
using ResultCode = Vortice.DXGI.ResultCode;

namespace HexaEngine.Windows
{
    public class DeviceManager : IDisposable
    {
        public static readonly string ProjectPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"));
        public static DeviceManager Current { get; private set; }

        private static readonly FeatureLevel[] FeatureLevels =
        {
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0
        };

        private readonly FeatureLevel _featureLevel;
#if D3D11On12
        private readonly IDXGIFactory4 _idxgiFactory;
#else
        private readonly IDXGIFactory2 _idxgiFactory;
#endif

        public DeviceManager(IRenderSurface window, Usage usage = Usage.RenderTargetOutput)
        {
#if DEBUG
            Trace.WriteLine("Initalizing DirectX");
#endif
            Current = this;
            RenderSurface = window;
            Width = window.Width;
            Height = window.Height;
            AspectRatio = (float)Width / Height;
            var swapChainDescription = new SwapChainDescription1
            {
                Width = window.Width,
                Height = window.Height,
                Format = Format.B8G8R8A8_UNorm,
                BufferCount = BufferCount,
                Usage = usage,
                SampleDescription = new SampleDescription(MSAASampleCount, MSAASampleQuality),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.Discard,
                Flags = SwapChainFlags.AllowModeSwitch,
            };

            var fullscreenDescription = new SwapChainFullscreenDescription
            {
                Windowed = !window.Fullscreen,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified
            };

            DXGI.CreateDXGIFactory1(out _idxgiFactory);

#if DEBUG
            Trace.WriteLine("Creating DXGIFactory");
#endif
            var adapter = GetHardwareAdapter();

#if DEBUG
            Trace.WriteLine("Creating DXGIAdapter");
            Trace.WriteLineIf(adapter == null, "Warning DXGIAdapter is null");
#endif

            var flags = DeviceCreationFlags.BgraSupport;
#if D3D_DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
#if D3D11On12

            Vortice.Direct3D12.D3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_12_1, out Vortice.Direct3D12.ID3D12Device dev12);
            ID3D12Device = dev12.QueryInterface<Vortice.Direct3D12.ID3D12Device5>();
            ID3D12CommandQueue = ID3D12Device.CreateCommandQueue<Vortice.Direct3D12.ID3D12CommandQueue>(Vortice.Direct3D12.CommandListType.Direct);
            SwapChain = IDXGIFactory.CreateSwapChainForHwnd(ID3D12CommandQueue, window.Handle, swapChainDescription, fullscreenDescription);
            // SwapChain = IDXGIFactory.CreateSwapChainForHwnd(ID3D12CommandQueue, window.Handle, swapChainDescription, fullscreenDescription);
            D3D11.D3D11On12CreateDevice(dev12, flags, FeatureLevels, new SharpGen.Runtime.IUnknown[] { ID3D12CommandQueue }, 0, out var tempDevice, out var tempContext, out _featureLevel);
            ID3D11Device = tempDevice.QueryInterface<ID3D11Device1>();
            ID3D11DeviceContext = tempContext.QueryInterface<ID3D11DeviceContext1>();
            tempContext.Dispose();
            tempDevice.Dispose();
            IDXGIFactory.MakeWindowAssociation(window.Handle, WindowAssociationFlags.IgnoreAll);
            IDXGIDevice = ID3D11Device.QueryInterface<IDXGIDevice>();
            BackBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
            RenderTargetView = ID3D11Device.CreateRenderTargetView(BackBuffer);
            BackBuffer.Dispose();
#else

#if DEBUG
            Trace.WriteLine("Creating ID3D11Device");
#endif

            D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, flags, FeatureLevels, out var tempDevice, out _featureLevel, out var tempContext);
#if DEBUG
            Trace.WriteLineIf(tempDevice == null, "Warning tempDevice is null");
            Trace.WriteLineIf(tempContext == null, "Warning tempContext is null");
#endif
            ID3D11Device = tempDevice.QueryInterface<ID3D11Device1>();
            ID3D11DeviceContext = tempContext.QueryInterface<ID3D11DeviceContext1>();
#if DEBUG
            Trace.WriteLineIf(ID3D11Device == null, "Warning ID3D11Device is null");
            Trace.WriteLineIf(ID3D11DeviceContext == null, "Warning ID3D11DeviceContext is null");
#endif
            ID3D11DeviceContext.DebugName = "DeviceMan." + nameof(ID3D11DeviceContext);
            tempContext.Dispose();
            tempDevice.Dispose();
#if DEBUG
            Trace.WriteLine("Machine-Info");
            Trace.WriteLine($"Processor: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}x{Environment.ProcessorCount}");
            Trace.WriteLine($"Device: {adapter.Description1.Description} FeatureLevel: {ID3D11Device.FeatureLevel} VRAM: {adapter.Description1.DedicatedVideoMemory / 1024 / 1024}");
            Trace.WriteLine($"ShaderCacheSupport: {ID3D11Device.CheckFeatureShaderCache().SupportFlags}");
            Trace.WriteLine(DebugFormatter.ToString(ID3D11Device.CheckFeatureOptions()));
            Trace.WriteLine(DebugFormatter.ToString(ID3D11Device.CheckFeatureOptions1()));
            Trace.WriteLine(DebugFormatter.ToString(ID3D11Device.CheckFeatureOptions2()));
            Trace.WriteLine($"VPAndRTArrayIndexFromAnyShaderFeedingRasterizer: {ID3D11Device.CheckFeatureOptions3().VPAndRTArrayIndexFromAnyShaderFeedingRasterizer}");
            Trace.WriteLine($"ExtendedNV12SharedTextureSupported: {ID3D11Device.CheckFeatureOptions4().ExtendedNV12SharedTextureSupported}");
            Trace.WriteLine($"SharedResourceTier: {ID3D11Device.CheckFeatureOptions5().SharedResourceTier}");
            Trace.WriteLine(ID3D11Device.CheckFeatureArchitectureInfo());
#endif

#if DEBUG
            Trace.WriteLine("Creating Swapchain");
#endif
            SwapChain = IDXGIFactory.CreateSwapChainForHwnd(ID3D11Device, window.Handle, swapChainDescription, fullscreenDescription);
            SwapChain.DebugName = "DeviceMan.SwapChain";
            IDXGIFactory.MakeWindowAssociation(window.Handle, WindowAssociationFlags.IgnoreAll);
            IDXGIDevice = ID3D11Device.QueryInterface<IDXGIDevice>();
            BackBuffer = SwapChain.GetBuffer<ID3D11Texture2D1>(0);
            BackBuffer.DebugName = "DeviceMan." + nameof(BackBuffer);
            RenderTargetView = ID3D11Device.CreateRenderTargetView(BackBuffer, new RenderTargetViewDescription() { Format = Format.B8G8R8A8_UNorm, ViewDimension = RenderTargetViewDimension.Texture2DMultisampled });
            RenderTargetView.DebugName = "DeviceMan." + nameof(RenderTargetView);

#if D2D1_SUPPORT
#if DEBUG
            Trace.WriteLine("Initalizing D2D1");
#endif
            ID2D1Device = Vortice.Direct2D1.D2D1.D2D1CreateDevice(IDXGIDevice, new Vortice.Direct2D1.CreationProperties()
            {
#if DEBUG
                DebugLevel = Vortice.Direct2D1.DebugLevel.Information,
#else
                DebugLevel = Vortice.Direct2D1.DebugLevel.None,
#endif
                Options = Vortice.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations,
                ThreadingMode = Vortice.Direct2D1.ThreadingMode.MultiThreaded
            });
            ID2D1Factory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory>();
            var temp = ID2D1Device.CreateDeviceContext(Vortice.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations);
            ID2D1DeviceContext = temp.QueryInterface<Vortice.Direct2D1.ID2D1DeviceContext1>();
            ID2D1DeviceContext.AntialiasMode = Vortice.Direct2D1.AntialiasMode.PerPrimitive;
            ID2D1DeviceContext.TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode.Cleartype;
            temp.Dispose();
            var surface = SwapChain.GetBuffer<IDXGISurface>(0);
            ID2D1RenderTarget = ID2D1DeviceContext.CreateBitmapFromDxgiSurface(surface, TargetBitmapProperties);
            surface.Dispose();
#if DWRITE_SUPPORT
            IDWriteFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<Vortice.DirectWrite.IDWriteFactory>();
#endif
#endif
            BackBuffer.Dispose();

#if D3D_DEBUG
            DebugDevice = ID3D11Device.QueryInterface<ID3D11Debug>();
#endif
#endif

            Texture2DDescription depthBufferDesc = new()
            {
                Width = Width,
                Height = Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float_S8X24_UInt,
                SampleDescription = new SampleDescription(MSAASampleCount, MSAASampleQuality),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            DepthStencilBuffer = ID3D11Device.CreateTexture2D(depthBufferDesc);
            DepthStencilBuffer.DebugName = "DeviceMan." + nameof(DepthStencilBuffer);

            DepthStencilDescription depthStencilDesc = new()
            {
                DepthEnable = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.LessEqual,
                StencilEnable = true,

                // Stencil operation if pixel front-facing.
                FrontFace = new DepthStencilOperationDescription()
                {
                    StencilFailOp = StencilOperation.Keep,
                    StencilDepthFailOp = StencilOperation.Increment,
                    StencilPassOp = StencilOperation.Keep,
                    StencilFunc = ComparisonFunction.Always
                },
                // Stencil operation if pixel is back-facing.
                BackFace = new DepthStencilOperationDescription()
                {
                    StencilFailOp = StencilOperation.Keep,
                    StencilDepthFailOp = StencilOperation.Increment,
                    StencilPassOp = StencilOperation.Keep,
                    StencilFunc = ComparisonFunction.Always
                }
            };

            DepthStencilDescription depthDisabledStencilDesc = new()
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.Less,
                StencilEnable = true,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF,
                // Stencil operation if pixel front-facing.
                FrontFace = new DepthStencilOperationDescription()
                {
                    StencilFailOp = StencilOperation.Keep,
                    StencilDepthFailOp = StencilOperation.Increment,
                    StencilPassOp = StencilOperation.Keep,
                    StencilFunc = ComparisonFunction.Always
                },
                // Stencil operation if pixel is back-facing.
                BackFace = new DepthStencilOperationDescription()
                {
                    StencilFailOp = StencilOperation.Keep,
                    StencilDepthFailOp = StencilOperation.Decrement,
                    StencilPassOp = StencilOperation.Keep,
                    StencilFunc = ComparisonFunction.Always
                }
            };

            DepthStencilDescription depthStencilDescSkybox = new()
            {
                DepthEnable = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.LessEqual,
                StencilEnable = true,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF,
                // Stencil operation if pixel front-facing.
                FrontFace = new DepthStencilOperationDescription()
                {
                    StencilFailOp = StencilOperation.Keep,
                    StencilDepthFailOp = StencilOperation.Increment,
                    StencilPassOp = StencilOperation.Keep,
                    StencilFunc = ComparisonFunction.Always
                },
                // Stencil operation if pixel is back-facing.
                BackFace = new DepthStencilOperationDescription()
                {
                    StencilFailOp = StencilOperation.Keep,
                    StencilDepthFailOp = StencilOperation.Decrement,
                    StencilPassOp = StencilOperation.Keep,
                    StencilFunc = ComparisonFunction.Always
                }
            };

            DepthStencilState = ID3D11Device.CreateDepthStencilState(depthStencilDesc);
            DepthStencilState.DebugName = "DeviceMan." + nameof(DepthStencilState);
            DepthStencilStateDisabled = ID3D11Device.CreateDepthStencilState(depthDisabledStencilDesc);
            DepthStencilStateDisabled.DebugName = "DeviceMan." + nameof(DepthStencilStateDisabled);
            DepthStencilStateSkybox = ID3D11Device.CreateDepthStencilState(depthStencilDescSkybox);
            DepthStencilStateSkybox.DebugName = "DeviceMan." + nameof(DepthStencilStateSkybox);
            ID3D11DeviceContext.OMSetDepthStencilState(DepthStencilState, 1);

            DepthStencilViewDescription depthStencilViewDesc = new()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ViewDimension = DepthStencilViewDimension.Texture2DMultisampled,
                Texture2D = new Texture2DDepthStencilView() { MipSlice = 0 }
            };

            DepthStencilView = ID3D11Device.CreateDepthStencilView(DepthStencilBuffer, depthStencilViewDesc);
            DepthStencilView.DebugName = "DeviceMan." + nameof(DepthStencilView);
            ID3D11DeviceContext.OMSetRenderTargets(RenderTargetView, DepthStencilView);

            RasterizerDescription rasterDesc = new()
            {
                AntialiasedLineEnable = true,
                CullMode = CullMode.Back,
                DepthBias = 0,
                DepthBiasClamp = .0f,
                DepthClipEnable = true,
                FillMode = FillMode.Solid,
                FrontCounterClockwise = false,
                MultisampleEnable = true,
                ScissorEnable = false,
                SlopeScaledDepthBias = .0f
            };

            var blendStateDesc = new BlendDescription();
            blendStateDesc.AlphaToCoverageEnable = true;
            blendStateDesc.IndependentBlendEnable = false;
            blendStateDesc.RenderTarget[0].IsBlendEnabled = false;
            blendStateDesc.RenderTarget[0].SourceBlend = Blend.One;
            blendStateDesc.RenderTarget[0].DestinationBlend = Blend.Zero;
            blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDesc.RenderTarget[0].SourceBlendAlpha = Blend.One;
            blendStateDesc.RenderTarget[0].DestinationBlendAlpha = Blend.Zero;
            blendStateDesc.RenderTarget[0].BlendOperationAlpha = BlendOperation.Add;
            blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteEnable.All;

            // Create the blend state using the description.
            AlphaEnableBlendingState = ID3D11Device.CreateBlendState(blendStateDesc);
            AlphaEnableBlendingState.DebugName = "DeviceMan." + nameof(AlphaEnableBlendingState);

            blendStateDesc.RenderTarget[0].IsBlendEnabled = false;

            AlphaDisableBlendingState = ID3D11Device.CreateBlendState(blendStateDesc);
            AlphaDisableBlendingState.DebugName = "DeviceMan." + nameof(AlphaDisableBlendingState);

            DefaultRasterizerState = ID3D11Device.CreateRasterizerState(rasterDesc);
            DefaultRasterizerState.DebugName = "DeviceMan." + nameof(DefaultRasterizerState);

            rasterDesc.CullMode = CullMode.None;
            NoCullingRasterizerState = ID3D11Device.CreateRasterizerState(rasterDesc);
            NoCullingRasterizerState.DebugName = "DeviceMan." + nameof(NoCullingRasterizerState);

            SetState(DefaultRasterizerState);
        }

        private bool disposedValue;

        public IRenderSurface RenderSurface { get; init; }

        public RenderWindow Window { get; init; }

        public AudioManager AudioManager { get; } = new();

        public int Width { get; private set; }

        public int Height { get; private set; }
#if D3D11On12
        public IDXGIFactory4 IDXGIFactory => _idxgiFactory;
#else
        public IDXGIFactory2 IDXGIFactory => _idxgiFactory;
#endif

        public FeatureLevel FeatureLevel => _featureLevel;

        public ID3D11DeviceContext1 ID3D11DeviceContext { get; private set; }

        public IDXGISwapChain SwapChain { get; private set; }

        public ID3D11RenderTargetView RenderTargetView { get; private set; }

        public IDXGIDevice IDXGIDevice { get; private set; }

        public ID3D11Device1 ID3D11Device { get; private set; }

        public ID3D11Texture2D BackBuffer { get; private set; }

        public ID3D11Texture2D DepthStencilBuffer { get; private set; }

        public ID3D11DepthStencilState DepthStencilState { get; private set; }

        public ID3D11DepthStencilState DepthStencilStateDisabled { get; private set; }

        public ID3D11DepthStencilState DepthStencilStateSkybox { get; private set; }

        public ID3D11DepthStencilView DepthStencilView { get; private set; }

        public ID3D11BlendState AlphaEnableBlendingState { get; private set; }

        public ID3D11BlendState AlphaDisableBlendingState { get; private set; }

        public ID3D11RasterizerState DefaultRasterizerState { get; private set; }

        public ID3D11RasterizerState NoCullingRasterizerState { get; private set; }

        public int MSAASampleCount { get; set; } = 2;

        public int MSAASampleQuality { get; set; } = 0;

#if D3D11On12

        public Vortice.Direct3D12.ID3D12Device5 ID3D12Device { get; }

        public Vortice.Direct3D12.ID3D12CommandQueue ID3D12CommandQueue { get; }
#endif

#if D2D1_SUPPORT

        public Vortice.Direct2D1.ID2D1Factory ID2D1Factory { get; }
        public Vortice.Direct2D1.ID2D1Device ID2D1Device { get; }
        public Vortice.Direct2D1.ID2D1DeviceContext1 ID2D1DeviceContext { get; }
        public Vortice.Direct2D1.ID2D1Bitmap1 ID2D1RenderTarget { get; set; }

        public static Vortice.DCommon.PixelFormat PixelFormat => new(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied);

        public static Vortice.Direct2D1.BitmapProperties1 TargetBitmapProperties => new(PixelFormat, 96, 96, Vortice.Direct2D1.BitmapOptions.Target | Vortice.Direct2D1.BitmapOptions.CannotDraw);

#if DWRITE_SUPPORT
        public Vortice.DirectWrite.IDWriteFactory IDWriteFactory { get; }
#endif
#endif

        public int BufferCount { get; set; } = 1;

        public float AspectRatio { get; private set; }

        public event EventHandler OnResize;

        public void Resize(int width, int height)
        {
            // Buffering values for reasons.
            Width = width;
            Height = height;
            AspectRatio = (float)Width / Height;

            // Delete all references to SwapChainBuffers.
            ID3D11DeviceContext.OMSetDepthStencilState(null);
            ID3D11DeviceContext.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
            ID3D11DeviceContext.ClearState();
            ID3D11DeviceContext.Flush();
            DepthStencilView.Dispose();
            RenderTargetView.Dispose();

            // Resize Targets and SwapChainBuffers.
            _ = SwapChain.ResizeTarget(new ModeDescription(Width, Height));
            _ = SwapChain.ResizeBuffers(BufferCount, Width, Height, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowModeSwitch);

            // Recreate SwapChainBuffer all references.
            BackBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
            RenderTargetView = ID3D11Device.CreateRenderTargetView(BackBuffer);
            BackBuffer.Dispose();

            Texture2DDescription depthBufferDesc = new()
            {
                Width = Width,
                Height = Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float_S8X24_UInt,
                SampleDescription = new SampleDescription(2, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            DepthStencilBuffer?.Dispose();
            DepthStencilBuffer = ID3D11Device.CreateTexture2D(depthBufferDesc);

            DepthStencilViewDescription depthStencilViewDesc = new()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ViewDimension = DepthStencilViewDimension.Texture2DMultisampled,
                Texture2D = new Texture2DDepthStencilView() { MipSlice = 0 }
            };

            ID3D11DeviceContext.OMSetDepthStencilState(DepthStencilState, 1);

            DepthStencilView = ID3D11Device.CreateDepthStencilView(DepthStencilBuffer, depthStencilViewDesc);

            ID3D11DeviceContext.OMSetRenderTargets(RenderTargetView, DepthStencilView);

            ID3D11DeviceContext.RSSetState(DefaultRasterizerState);

            OnResize?.Invoke(this, null);
        }

        public void SwitchDepth(bool state)
        {
            if (state)
            {
                ID3D11DeviceContext.OMSetDepthStencilState(DepthStencilState, 1);
            }
            else
            {
                ID3D11DeviceContext.OMSetDepthStencilState(DepthStencilStateDisabled, 1);
            }
        }

        public void SwitchAlpha(bool state)
        {
            var blendFactor = Color.FromArgb(0, 0, 0, 0);
            if (state)
            {
                // Turn on the alpha blending.
                ID3D11DeviceContext.OMSetBlendState(AlphaEnableBlendingState, blendFactor, 1);
            }
            else
            {
                ID3D11DeviceContext.OMSetBlendState(AlphaDisableBlendingState, blendFactor, 1);
            }
        }

        public ID3D11RasterizerState CurrentRasterizerState { get; private set; }

        public ID3D11Debug DebugDevice { get; }

        public ID3D11RasterizerState CreateRasterizerState(RasterizerDescription description)
        {
            return ID3D11Device.CreateRasterizerState(description);
        }

        public void SetStencil(ID3D11DepthStencilState state)
        {
            ID3D11DeviceContext.OMSetDepthStencilState(state);
        }

        public void RestoreStencil()
        {
            ID3D11DeviceContext.OMSetDepthStencilState(DepthStencilState);
        }

        public void SetState(ID3D11RasterizerState state)
        {
            ID3D11DeviceContext.RSSetState(state);
            CurrentRasterizerState = state;
        }

        public void RestoreState()
        {
            SetState(DefaultRasterizerState);
        }

        public void ClearRenderTarget()
        {
            ID3D11DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);
            ID3D11DeviceContext.ClearRenderTargetView(RenderTargetView, RenderSurface.BackgroundClear);
        }

        public void SetRenderTarget()
        {
            ID3D11DeviceContext.OMSetRenderTargets(RenderTargetView, DepthStencilView);
            ID3D11DeviceContext.RSSetViewport(RenderSurface.ViewOffsetX, RenderSurface.ViewOffsetY, RenderSurface.ViewWidth, RenderSurface.ViewHeight, 0, 1);
        }

        private IDXGIAdapter1 GetHardwareAdapter()
        {
            IDXGIAdapter1 adapter = null;
            var factory6 = IDXGIFactory.QueryInterfaceOrNull<IDXGIFactory6>();
            if (factory6 != null)
            {
                for (var adapterIndex = 0;
                    factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out adapter) !=
                    ResultCode.NotFound;
                    adapterIndex++)
                {
                    Trace.WriteLine($"Found Adapter {adapter.Description1.Description}");
                    var desc = adapter.Description1;

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
                for (var adapterIndex = 0;
                    IDXGIFactory.EnumAdapters1(adapterIndex, out adapter) != ResultCode.NotFound;
                    adapterIndex++)
                {
                    var desc = adapter.Description1;

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ID3D11DeviceContext.ClearState();

                DepthStencilBuffer.Dispose();
                DepthStencilBuffer = null;
                DepthStencilState.Dispose();
                DepthStencilState = null;
                DepthStencilStateDisabled.Dispose();
                DepthStencilStateDisabled = null;
                DepthStencilStateSkybox.Dispose();
                DepthStencilStateSkybox = null;
                NoCullingRasterizerState.Dispose();
                NoCullingRasterizerState = null;
                DepthStencilView.Dispose();
                DepthStencilView = null;
                AlphaEnableBlendingState.Dispose();
                AlphaEnableBlendingState = null;
                AlphaDisableBlendingState.Dispose();
                AlphaDisableBlendingState = null;
                DefaultRasterizerState.Dispose();
                DefaultRasterizerState = null;
                RenderTargetView.Dispose();
                RenderTargetView = null;
                BackBuffer.Dispose();
                BackBuffer = null;
                ID3D11DeviceContext.Dispose();
                ID3D11DeviceContext = null;
                SwapChain.Dispose();
                SwapChain = null;
                ID3D11Device.Dispose();
                ID3D11Device = null;
                IDXGIFactory.Dispose();
                IDXGIDevice.Dispose();
#if D2D1_SUPPORT

                ID2D1RenderTarget.Dispose();
                ID2D1Device.Dispose();
                ID2D1DeviceContext.Dispose();
#if DWRITE_SUPPORT
                IDWriteFactory.Dispose();
#endif
#endif

                DebugDevice?.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
                DebugDevice?.Dispose();
                GC.Collect(2, GCCollectionMode.Forced);
                Debug.WriteLine("Closing");
                disposedValue = true;
            }
        }

        ~DeviceManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}