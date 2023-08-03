namespace VoxelEngine.Rendering.D3D
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.Direct3D11.Debug;
    using Vortice.DXGI;

    public static class D3D11DeviceManager
    {
        internal static readonly FeatureLevel[] FeatureLevels =
        {
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
        };

        private static FeatureLevel _featureLevel;
        private static ID3D11Device1 iD3D11Device;
        private static ID3D11DeviceContext1 iD3D11DeviceContext;

        public static ID3D11Device1 ID3D11Device => iD3D11Device;

        public static ID3D11DeviceContext1 ID3D11DeviceContext => iD3D11DeviceContext;

        public static FeatureLevel FeatureLevel => _featureLevel;

#if D3D_DEBUG
        public static ID3D11Debug DebugDevice { get; private set; }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeDevice(IDXGIAdapter4 adapter)
        {
            DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;
#if D3D_DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif

#if D3D11On12
            D3D11On12DeviceManager.InitializeDevice(adapter, flags, FeatureLevels, out iD3D11Device, out iD3D11DeviceContext, out _featureLevel);
#else

            D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, flags, FeatureLevels, out ID3D11Device tempDevice, out _featureLevel, out ID3D11DeviceContext tempContext);
            iD3D11Device = tempDevice.QueryInterface<ID3D11Device1>();
            iD3D11Device.DebugName = nameof(ID3D11Device);
            iD3D11DeviceContext = tempContext.QueryInterface<ID3D11DeviceContext1>();
            iD3D11DeviceContext.DebugName = nameof(ID3D11DeviceContext);
            tempContext.Dispose();
            tempDevice.Dispose();

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
            DebugDevice.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
            Debug.WriteLine("END REPORT" + Environment.NewLine);
#endif
            iD3D11DeviceContext.UnsetSOTargets();
            iD3D11DeviceContext.ClearState();
            iD3D11DeviceContext.Flush();

#if D3D_DEBUG
            Debug.WriteLine("BEGIN REPORT AFTER FLUSH");
            DebugDevice.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
            Debug.WriteLine("END REPORT AFTER FLUSH" + Environment.NewLine);
#endif

            iD3D11DeviceContext.Dispose();
            iD3D11DeviceContext = null;

            iD3D11Device.Dispose();
            iD3D11Device = null;

#if D3D_DEBUG
            Debug.WriteLine("BEGIN REPORT BEFORE TERMINATE");
            DebugDevice.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
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