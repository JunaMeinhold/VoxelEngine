// Copyright (c) 2022 Juna Meinhold
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace App.Renderers
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using App.Pipelines.Forward;
    using Vortice.Direct3D11;
    using VoxelEngine;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Lightning;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Objects.Primitives;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Rendering.DXGI;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using Window = VoxelEngine.Core.Window;

    public class MainSceneDeferredRenderer : ISceneRenderer
    {
        private ChunkPrepassPipeline chunkPrepass;
        private CSMChunkPipeline chunkDepthPrepassCSM;
        private LightPipeline lightPipeline;
        private FXAAEffect fxaa;

        private Rectangle rectangle;

        private ID3D11SamplerState anisotropicClamp;

        private DepthStencil depthStencil;
        private RenderTextureArray gbuffer;
        private RenderTexture fxaaBuffer;
        private RenderTexture csmBuffer;

        private DirectionalLight directionalLight;
        private CBDirectionalLightSD directionalLightCB = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, Window window)
        {
            anisotropicClamp = device.CreateSamplerState(SamplerDescription.AnisotropicClamp);

            depthStencil = new(device, window.Width, window.Height);
            gbuffer = new(device, window.Width, window.Height, count: 4);
            gbuffer.RenderTargets.DepthStencil = depthStencil;
            fxaaBuffer = new(device, window.Width, window.Height);
            csmBuffer = new(device, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, 5, Vortice.DXGI.Format.R32_Float, true);

            rectangle = new();

            lightPipeline = new(device);
            lightPipeline.ShaderResourceViews.AppendRange(gbuffer, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(csmBuffer, ShaderStage.Pixel);
            lightPipeline.SamplerStates.Add(anisotropicClamp, ShaderStage.Pixel, 4);

            fxaa = new(device);
            fxaa.ShaderResourceViews.Append(fxaaBuffer, ShaderStage.Pixel);
            fxaa.SamplerStates.Append(anisotropicClamp, ShaderStage.Pixel);

            chunkPrepass = new(device);
            chunkDepthPrepassCSM = new(device);

            directionalLight = new();
            directionalLight.Transform.Far = 100;
            directionalLight.Transform.Rotation = new(0, 90, 0);
            Keyboard.OnKeyUp += Keyboard_OnKeyUp;
        }

        private void Keyboard_OnKeyUp(object sender, VoxelEngine.Core.Input.Events.KeyboardEventArgs e)
        {
            if (e.KeyCode == Silk.NET.SDL.KeyCode.KF1)
            {
                if (Time.TimeScale > 0)
                {
                    Time.TimeScale = 0;
                }
                else
                {
                    Time.TimeScale = 60 * 10;
                }
            }
        }

        public void Resize(ID3D11Device device, Window window)
        {
            depthStencil.Dispose();
            gbuffer.Dispose();
            fxaaBuffer.Dispose();

            depthStencil = new(device, window.Width, window.Height);
            gbuffer = new(device, window.Width, window.Height, 4);
            gbuffer.RenderTargets.DepthStencil = depthStencil;
            fxaaBuffer = new(device, window.Width, window.Height);

            lightPipeline.ShaderResourceViews.Clear();
            lightPipeline.ShaderResourceViews.AppendRange(gbuffer, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(csmBuffer, ShaderStage.Pixel);

            fxaa.ShaderResourceViews.Clear();
            fxaa.ShaderResourceViews.Append(fxaaBuffer, ShaderStage.Pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Render(ID3D11DeviceContext context, Camera view, SceneElementCollection elements)
        {
            Vector3 rot = directionalLight.Transform.Rotation;
            rot.Y = 360 * Time.GameTimeNormalized - 90;
            float ro = (rot.Y - 180F).NormalizeEulerAngleDegrees();
            directionalLight.Transform.Rotation = rot.NormalizeEulerAngleDegrees();
            if (rot.Y > 45 && rot.Y < 135)
            {
                directionalLight.Color = Vector4.One;
            }
            else if (rot.Y > 135 && rot.Y < 225)
            {
                directionalLight.Color = Vector4.One * MathUtil.Lerp(1, 0.2f, (rot.Y - 135) / 90);
            }
            else if (ro > 45 && ro < 135)
            {
                directionalLight.Color = Vector4.One * 0.2f;
            }
            else if (ro > 135 && ro < 225)
            {
                directionalLight.Color = Vector4.One * MathUtil.Lerp(0.2f, 1, (ro - 135) / 90);
            }

            Frustum cameraFrustum = view.Transform.Frustum;

            // Depth light pass.
            csmBuffer.RenderTarget.ClearTarget(context);
            gbuffer.RenderTargets.ClearTarget(context);
            csmBuffer.RenderTarget.DepthStencil.ClearDepthStencil(context);
            gbuffer.RenderTargets.DepthStencil.ClearDepthStencil(context);

            context.ClearState();

            for (int i = 0; i < elements.Count; i++)
            {
                SceneElement element = elements[i];
                if (element is World world)
                {
                    CSMHelper.GetLightSpaceMatrices(directionalLightCB.Views, directionalLightCB.Cascades, view, directionalLight.Transform, 5);
                    directionalLightCB.Color = directionalLight.Color;
                    directionalLightCB.Direction = directionalLight.Transform.Forward;
                    chunkDepthPrepassCSM.Update(context, directionalLightCB.Views);
                    chunkDepthPrepassCSM.BeginDraw(context);
                    csmBuffer.RenderTarget.SetTarget(context);
                    for (int j = 0; j < world.LoadedChunks.Count; j++)
                    {
                        Chunk chunk = world.LoadedChunks[j];
                        if (chunk.VertexBuffer is not null && chunk.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(chunk.BoundingBox))
                        {
                            chunkDepthPrepassCSM.Update(context, chunk);
                            chunk.Bind(context);
                            chunkDepthPrepassCSM.DrawFast(context, chunk.VertexBuffer.VertexCount, 0);
                        }
                    }
                }
            }

            context.ClearState();

            // Deferred fill GBuffers pass.
            for (int i = 0; i < elements.Count; i++)
            {
                SceneElement element = elements[i];
                if (element is World world)
                {
                    chunkPrepass.BeginDraw(context);
                    gbuffer.RenderTargets.SetTarget(context);
                    for (int j = 0; j < world.LoadedChunks.Count; j++)
                    {
                        Chunk chunk = world.LoadedChunks[j];
                        if (chunk.VertexBuffer is not null && chunk.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(chunk.BoundingBox))
                        {
                            chunkPrepass.Update(context, view, chunk);
                            chunk.Bind(context);
                            chunkPrepass.DrawFast(context, chunk.VertexBuffer.VertexCount, 0);
                        }
                    }
                }
            }

            context.ClearState();

            // Deferred second pass
            lightPipeline.Update(context, new(view), directionalLightCB);
            fxaaBuffer.RenderTarget.ClearAndSetTarget(context);
            rectangle.DrawAuto(context, lightPipeline);

            context.ClearState();

            DXGIDeviceManager.SwapChain.ClearAndSetTarget(context);
            rectangle.DrawAuto(context, fxaa);
            DXGIDeviceManager.SwapChain.DepthStencil = depthStencil;

            context.ClearState();

            // Forward pass.
            for (int i = 0; i < elements.ForwardComponents.Count; i++)
            {
                DXGIDeviceManager.SwapChain.SetTarget(context);
                IForwardRenderComponent element = elements.ForwardComponents[i];
                element.DrawForward(context, view);
            }

            DXGIDeviceManager.SwapChain.DepthStencil = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            chunkPrepass.Dispose();
            chunkDepthPrepassCSM.Dispose();
            lightPipeline.Dispose();
            fxaa.Dispose();
            rectangle.Dispose();
            anisotropicClamp.Dispose();
            depthStencil.Dispose();
            gbuffer.Dispose();
            fxaaBuffer.Dispose();
            csmBuffer.Dispose();
        }
    }
}