using VoxelEngine.Rendering.D3D.Shaders;

namespace App.Renderers.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Forward;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Objects.Primitives;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Scenes;
    using ShaderStage = ShaderStage;

    public class SkyboxRenderer : IForwardRenderComponent
    {
        private GameObject sceneElement;
        private ShaderPipeline<SkyboxPipeline> pipeline;
        public Texture2D Texture;
        public UVSphere sphere;
        public string TexturePath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameObject element)
        {
            sceneElement = element;
            pipeline = new(device);
            sphere = new();
            Texture = new();
            Texture.Load(device, TexturePath);
            Texture.Sampler = device.CreateSamplerState(SamplerDescription.LinearClamp);
            Texture.Add(new(ShaderStage.Pixel, 0));
            pipeline.ShaderResources.Add(Texture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ID3D11DeviceContext context, IView view)
        {
            sphere.Bind(context);
            pipeline.DrawIndexed(context, view, sceneElement.Transform, sphere.IndexBuffer.IndexCount, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            pipeline.Dispose();
            pipeline = null;
            sphere.Dispose();
            Texture.Dispose();
            Texture = null;
            sceneElement = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoundingBox GetBoundingBox()
        {
            return new BoundingBox(new Vector3(float.NaN), new Vector3(float.NaN));
        }
    }
}