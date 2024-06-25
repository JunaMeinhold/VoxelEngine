namespace App.Renderers.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Forward;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Objects.Primitives;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public class BlockHighlightRenderer : IForwardRenderComponent
    {
        private World _world;
        private ShaderPipeline<LinePipeline> linePipeline;
        private LineBox lineBox;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameObject element)
        {
            if (element is World world)
            {
                _world = world;
            }

            linePipeline = new(device);
            linePipeline.ShaderLogic.Color = Vortice.Mathematics.Colors.Gray;
            lineBox = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ID3D11DeviceContext context, IView view)
        {
            if (_world.Player.IsLookingAtBlock)
            {
                lineBox.Bind(context);
                linePipeline.DrawIndexed(context, view, Matrix4x4.CreateScale(0.5f) * Matrix4x4.CreateTranslation(_world.Player.LookAtBlock + new Vector3(0.5f)), lineBox.IndexBuffer.IndexCount, 0, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            linePipeline.Dispose();
            linePipeline = null;
            lineBox.Dispose();
            lineBox = null;
            _world = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoundingBox GetBoundingBox()
        {
            return new BoundingBox(new Vector3(float.NaN), new Vector3(float.NaN));
        }
    }
}