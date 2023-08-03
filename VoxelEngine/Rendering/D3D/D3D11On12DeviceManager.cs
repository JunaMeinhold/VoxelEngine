namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using SharpGen.Runtime;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.Direct3D11on12;
    using Vortice.Direct3D12;
    using Vortice.DXGI;

    public class D3D11On12DeviceManager
    {
        public static ID3D12Device ID3D12Device { get; private set; }

        public static ID3D12CommandQueue ID3D12CommandQueue { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeDevice(IDXGIAdapter4 adapter, DeviceCreationFlags flags, FeatureLevel[] featureLevels, out ID3D11Device1 device, out ID3D11DeviceContext1 context, out FeatureLevel _featureLevel)
        {
            D3D12.D3D12CreateDevice(adapter, out ID3D12Device device12);
            ID3D12Device = device12;
            ID3D12CommandQueue = device12.CreateCommandQueue(CommandListType.Direct);
            Apis.D3D11On12CreateDevice(device12, flags, featureLevels, new IUnknown[] { ID3D12CommandQueue }, 1, out ID3D11Device tempDevice, out ID3D11DeviceContext tempContext, out _featureLevel);
            device = tempDevice.QueryInterface<ID3D11Device1>();
            context = tempContext.QueryInterface<ID3D11DeviceContext1>();
            tempContext.Dispose();
            tempDevice.Dispose();
        }

        public static void Dispose()
        {
            ID3D12Device.Dispose();
            ID3D12CommandQueue.Dispose();
        }
    }
}