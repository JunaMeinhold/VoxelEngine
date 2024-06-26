﻿using VoxelEngine.Graphics.Shaders;

namespace App.Renderers.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Forward;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Scenes;
    using ShaderStage = ShaderStage;

    public class TextureRenderer : IForwardRenderComponent
    {
        private TexturePipeline pipeline;
        private ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private ID3D11SamplerState samplerState;
        public VertexBuffer<OrthoVertex> VertexBuffer;
        public Texture2D Texture;
        public string TexturePath;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameObject element)
        {
            Texture = new();
            Texture.Load(device, TexturePath);

            mvpBuffer = new(device, CpuAccessFlags.Write);
            samplerState = device.CreateSamplerState(SamplerDescription.LinearClamp);
            pipeline = new(device);
            pipeline.ConstantBuffers.Add(mvpBuffer, ShaderStage.Vertex, 0);
            pipeline.ShaderResourceViews.Add(Texture.SRV, ShaderStage.Pixel, 0);
            pipeline.SamplerStates.Add(samplerState, ShaderStage.Pixel, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ID3D11DeviceContext context, IView view)
        {
            // VertexBuffer.Bind(context);
            // mvpBuffer.Update(context, new ModelViewProjBuffer(view, Matrix4x4.Identity));
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