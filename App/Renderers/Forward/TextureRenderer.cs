using VoxelEngine.Graphics.D3D11;

namespace App.Renderers.Forward
{
    using App.Pipelines.Forward;
    using Hexa.NET.D3D11;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Graphics.D3D11.Interfaces;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class TextureRenderer : IForwardRenderComponent
    {
        private TexturePipeline pipeline;
        private ConstantBuffer<Matrix4x4> mvpBuffer;
        public VertexBuffer<OrthoVertex> VertexBuffer;
        public Texture2D Texture;
        public string TexturePath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(GameObject element)
        {
            Texture = new(TexturePath);

            mvpBuffer = new(CpuAccessFlag.Write);
            pipeline = new();
            pipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            pipeline.Bindings.SetSRV("texture", Texture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ComPtr<ID3D11DeviceContext> context, IView view)
        {
            // VertexBuffer.Bind(context);
            //mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.Identity));
            // pipeline.Pass(context, ScreenCamera.Instance, Matrix4x4.Identity, VertexBuffer.Count, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            pipeline.Dispose();
            VertexBuffer.Dispose();
            VertexBuffer = null;
            Texture.Dispose();
            TexturePath = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoundingBox GetBoundingBox()
        {
            return new BoundingBox(new Vector3(float.NaN), new Vector3(float.NaN));
        }
    }
}