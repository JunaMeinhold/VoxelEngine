namespace VoxelEngine.Rendering.D3D
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;

    public static unsafe class D3D11DeviceManager
    {
        public static readonly D3D11 D3D11 = D3D11.GetApi();

        private static D3DFeatureLevel _featureLevel;
        private static ComPtr<ID3D11Device5> iD3D11Device;
        private static ComPtr<ID3D11DeviceContext4> iD3D11DeviceContext;

        public static ComPtr<ID3D11Device5> ID3D11Device => iD3D11Device;

        public static ComPtr<ID3D11DeviceContext4> ID3D11DeviceContext => iD3D11DeviceContext;

        public static D3DFeatureLevel FeatureLevel => _featureLevel;

#if D3D_DEBUG
        public static ComPtr<ID3D11Debug> DebugDevice { get; private set; }

#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeDevice(ComPtr<IDXGIAdapter4> adapter)
        {
            CreateDeviceFlag flags = CreateDeviceFlag.BgraSupport;
#if D3D_DEBUG
            flags |= CreateDeviceFlag.Debug;
#endif

#if D3D11On12
            D3D11On12DeviceManager.InitializeDevice(adapter, flags, FeatureLevels, out iD3D11Device, out iD3D11DeviceContext, out _featureLevel);
#else
            const int featureLevelCount = 5;
            D3DFeatureLevel* featureLevels = stackalloc D3DFeatureLevel[]
            {
                D3DFeatureLevel.Level122,
                D3DFeatureLevel.Level121,
                D3DFeatureLevel.Level120,
                D3DFeatureLevel.Level111,
                D3DFeatureLevel.Level110,
            };
            D3DFeatureLevel featureLevel;
            ID3D11Device* tempDevice;
            ID3D11DeviceContext* tempContext;
            D3D11.CreateDevice((IDXGIAdapter*)adapter.Handle, D3DDriverType.Unknown, 0, (uint)flags, featureLevels, featureLevelCount, 0, &tempDevice, &featureLevel, &tempContext);
            _featureLevel = featureLevel;
            iD3D11Device = tempDevice->QueryInterface<ID3D11Device5>();
            SetDebugName(iD3D11Device, nameof(ID3D11Device));
            iD3D11DeviceContext = tempContext->QueryInterface<ID3D11DeviceContext4>();
            SetDebugName(iD3D11DeviceContext, nameof(ID3D11DeviceContext));
            tempContext->Release();
            tempDevice->Release();

#endif

#if D3D_DEBUG
            DebugDevice = ID3D11Device.QueryInterface<ID3D11Debug>();
#endif
        }

        public static void ResizeBegin()
        {
            // Delete all references to SwapChainBuffers.
            ID3D11DeviceContext.ClearState();
            ID3D11DeviceContext.Flush();
        }

        public static void ResizeEnd()
        {
        }

        public static void Initialize()
        {
        }

        public static void Dispose()
        {
#if D3D_DEBUG
            Debug.WriteLine("BEGIN REPORT");
            DebugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
            Debug.WriteLine("END REPORT" + Environment.NewLine);
#endif

            iD3D11DeviceContext.ClearState();
            iD3D11DeviceContext.Flush();

#if D3D_DEBUG
            Debug.WriteLine("BEGIN REPORT AFTER FLUSH");
            DebugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
            Debug.WriteLine("END REPORT AFTER FLUSH" + Environment.NewLine);
#endif

            iD3D11DeviceContext.Dispose();
            iD3D11DeviceContext = null;

            iD3D11Device.Dispose();
            iD3D11Device = null;

#if D3D_DEBUG
            Debug.WriteLine("BEGIN REPORT BEFORE TERMINATE");
            DebugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
            Debug.WriteLine("END REPORT BEFORE TERMINATE" + Environment.NewLine);
            DebugDevice.Dispose();
            DebugDevice = null;
#endif
        }

        public static void DisposeLate()
        {
        }
    }
}