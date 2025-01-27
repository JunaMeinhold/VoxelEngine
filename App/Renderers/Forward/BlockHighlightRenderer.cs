namespace App.Renderers.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Forward;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Primitives;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using BoundingBox = Hexa.NET.Mathematics.BoundingBox;

    public class BlockHighlightRenderer : IForwardRenderComponent
    {
        private ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private ConstantBuffer<Color4> colorBuffer;
        private World _world;
        private LinePipeline linePipeline;
        private LineBox lineBox;

        public Color4 Color;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameObject element)
        {
            if (element is World world)
            {
                _world = world;
            }
            mvpBuffer = new(device, CpuAccessFlags.Write);
            colorBuffer = new(device, CpuAccessFlags.Write);
            linePipeline = new(device);
            linePipeline.ConstantBuffers.Append(mvpBuffer, ShaderStage.Vertex);
            linePipeline.ConstantBuffers.Append(colorBuffer, ShaderStage.Pixel);
            Color = Colors.Gray;
            lineBox = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ID3D11DeviceContext context, IView view)
        {
            if (_world.Player.IsLookingAtBlock)
            {
                mvpBuffer.Update(context, new ModelViewProjBuffer(view, Matrix4x4.CreateScale(0.5f) * Matrix4x4.CreateTranslation(_world.Player.LookAtBlock + new Vector3(0.5f))));
                colorBuffer.Update(context, Color);

                lineBox.Bind(context);
                linePipeline.Begin(context);
                context.DrawIndexed(lineBox.IndexBuffer.Count, 0, 0);
                linePipeline.End(context);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            linePipeline.Dispose();
            linePipeline = null;
            mvpBuffer.Dispose();
            mvpBuffer = null;
            colorBuffer.Dispose();
            colorBuffer = null;
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