namespace App.Renderers.Forward
{
    using App.Pipelines.Forward;
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class TextureRenderer : BaseRenderComponent
    {
        private TexturePipeline pipeline;
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        public VertexBuffer<OrthoVertex> VertexBuffer;
        public Texture2D Texture;
        public string TexturePath;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Overlay;

        public override void Awake()
        {
            Texture = new(TexturePath);

            mvpBuffer = new(CpuAccessFlag.Write);
            pipeline = new();
            pipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            pipeline.Bindings.SetSRV("tex", Texture);
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
            //VertexBuffer.Bind(context);
            //mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.Identity));
            //pipeline.Pass(context, ScreenCamera.Instance, Matrix4x4.Identity, VertexBuffer.Count, 0);
        }

        public override void Destroy()
        {
            pipeline.Dispose();
            mvpBuffer.Dispose();
            VertexBuffer.Dispose();
            Texture.Dispose();
        }
    }
}