using VoxelEngine.Scenes;

namespace VoxelEngine.Graphics
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Graphics.D3D11.Interfaces;
    using VoxelEngine.Windows;

    public interface IDepthRenderComponent : IComponent
    {
        /// <summary>
        /// Draws the SceneElement.<br/>
        /// Called by <seealso cref="ISceneRenderer.Render(IView, SceneElementCollection)"/> ... <seealso cref="Scene.Render"/> ... <seealso cref="GameWindow.RenderVoid"/><br/>
        /// </summary>
        /// <param name="view">The current camera.</param>
        void DrawDepth(ComPtr<ID3D11DeviceContext> context, IRenderTarget target, IView view, IView light);
    }
}