namespace App.Renderers
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Events;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.DXGI;
    using VoxelEngine.Scenes;

    public class MainMenuRenderer : ISceneRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, Window window)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Render(ID3D11DeviceContext context, Camera view, SceneElementCollection elements)
        {
            DXGIDeviceManager.SwapChain.ClearAndSetTarget(context);
        }

        public void Resize(ID3D11Device device, Window window)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
        }
    }
}