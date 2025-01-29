namespace App.Renderers.Forward
{
    using App.Pipelines.Forward;
    using Hexa.NET.D3D11;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Primitives;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;

    public class BlockHighlightRenderer : BaseRenderComponent
    {
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private ConstantBuffer<Vector4> colorBuffer;
        private World _world;
        private LinePipeline linePipeline;
        private LineBox lineBox;

        public Vector4 Color;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.GeometryLast;

        public override void Awake()
        {
            if (GameObject is World world)
            {
                _world = world;
            }
            mvpBuffer = new(CpuAccessFlag.Write);
            colorBuffer = new(CpuAccessFlag.Write);
            linePipeline = new();
            linePipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            linePipeline.Bindings.SetCBV("ColorBuffer", colorBuffer);
            Color = Colors.Gray;
            lineBox = new();
        }

        public override void Draw(ComPtr<ID3D11DeviceContext> context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.ForwardPass)
            {
                DrawForward(context);
            }
        }

        public void DrawForward(ComPtr<ID3D11DeviceContext> context)
        {
            if (_world.Player.IsLookingAtBlock)
            {
                mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateScale(0.5f) * Matrix4x4.CreateTranslation(_world.Player.LookAtBlock + new Vector3(0.5f))));
                colorBuffer.Update(context, Color);

                lineBox.Bind(context);
                linePipeline.Begin(context);
                context.DrawIndexed((uint)lineBox.IndexBuffer.Count, 0, 0);
                linePipeline.End(context);
            }
        }

        public override void Destroy()
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
    }
}