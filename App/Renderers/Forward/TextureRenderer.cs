using VoxelEngine.Rendering.D3D.Shaders;

namespace App.Renderers.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Forward;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Scenes;
    using ShaderStage = ShaderStage;

    public class TextureRenderer : IForwardRenderComponent
    {
        private ShaderPipeline<TexturePipeline> pipeline;
        public VertexBuffer<OrthoVertex> VertexBuffer;
        public Texture2D Texture;
        public string TexturePath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameObject element)
        {
            Texture = new();
            Texture.Load(device, TexturePath);
            Texture.Add(new(ShaderStage.Pixel, 0));
            Texture.Sampler = device.CreateSamplerState(SamplerDescription.LinearClamp);
            pipeline = new(device);
            pipeline.ShaderResources.Add(Texture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ID3D11DeviceContext context, IView view)
        {
            // VertexBuffer.Bind(context);
            // pipeline.Draw(context, ScreenCamera.Instance, Matrix4x4.Identity, VertexBuffer.VertexCount, 0);
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