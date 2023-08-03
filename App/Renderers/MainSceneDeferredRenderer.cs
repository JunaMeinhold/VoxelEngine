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

using VoxelEngine.Core.Windows;

namespace App.Renderers
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using App.Pipelines.Forward;
    using HexaEngine.Editor;
    using HexaEngine.ImGuiNET;
    using Vortice.Direct3D11;
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
    using VoxelEngine.Windows;

    public class MainSceneDeferredRenderer : ISceneRenderer
    {
        private SwapChain swapChain;
        private ChunkPrepassPipeline chunkPrepass;
        private CSMChunkPipeline chunkDepthPrepassCSM;
        private LightPipeline lightPipeline;
        private ComposeEffect compose;
        private FXAAEffect fxaa;

        private Rectangle rectangle;

        private ID3D11SamplerState anisotropicClamp;
        private ID3D11SamplerState pointClamp;
        private ID3D11SamplerState linearClamp;

        private DepthStencil depthStencil;
        private RenderTextureArray gbuffer;
        private RenderTexture lightBuffer;
        private RenderTexture fxaaBuffer;
        private DepthStencil csmBuffer;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[16];

        private DirectionalLight directionalLight;
        private CBDirectionalLightSD directionalLightCB = new();
        private int width;
        private int height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameWindow window)
        {
            for (int i = 0; i < 16; i++)
            {
                ShadowFrustra[i] = new();
            }
            swapChain = window.SwapChain;
            width = window.Width;
            height = window.Height;

            anisotropicClamp = device.CreateSamplerState(SamplerDescription.AnisotropicClamp);
            pointClamp = device.CreateSamplerState(SamplerDescription.PointClamp);
            linearClamp = device.CreateSamplerState(SamplerDescription.LinearClamp);

            depthStencil = new(device, window.Width, window.Height);
            gbuffer = new(device, window.Width, window.Height, Vortice.DXGI.Format.R16G16B16A16_Float, Vortice.DXGI.Format.R8G8B8A8_UNorm, Vortice.DXGI.Format.R16G16B16A16_Float, Vortice.DXGI.Format.R16G16B16A16_Float);
            gbuffer.RenderTargets.DepthStencil = depthStencil;
            lightBuffer = new(device, width, height, 1, Vortice.DXGI.Format.R16G16B16A16_Float);
            fxaaBuffer = new(device, window.Width, window.Height);
            csmBuffer = new(device, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, 5, Vortice.DXGI.Format.R32_Float, true);

            rectangle = new();

            lightPipeline = new(device);
            lightPipeline.ShaderResourceViews.AppendRange(gbuffer, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(depthStencil.SRV, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(csmBuffer.SRV, ShaderStage.Pixel);
            lightPipeline.SamplerStates.Add(linearClamp, ShaderStage.Pixel, 0);
            lightPipeline.SamplerStates.Add(linearClamp, ShaderStage.Pixel, 4);

            compose = new(device);
            compose.ShaderResourceViews.Append(lightBuffer, ShaderStage.Pixel);
            compose.SamplerStates.Append(linearClamp, ShaderStage.Pixel);

            fxaa = new(device);
            fxaa.ShaderResourceViews.Append(fxaaBuffer, ShaderStage.Pixel);
            fxaa.SamplerStates.Append(linearClamp, ShaderStage.Pixel);

            chunkPrepass = new(device);
            chunkDepthPrepassCSM = new(device);

            directionalLight = new();
            directionalLight.Transform.Far = 100;
            directionalLight.Transform.Rotation = new(0, 90, 0);
            Keyboard.KeyUp += Keyboard_OnKeyUp;
        }

        private void Keyboard_OnKeyUp(object sender, VoxelEngine.Core.Input.Events.KeyboardEventArgs e)
        {
            if (e.KeyCode == Key.F1)
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

        public void Resize(ID3D11Device device, GameWindow window)
        {
            width = window.Width;
            height = window.Height;
            depthStencil.Dispose();
            gbuffer.Dispose();
            lightBuffer.Dispose();
            fxaaBuffer.Dispose();

            depthStencil = new(device, window.Width, window.Height);
            gbuffer = new(device, window.Width, window.Height, 4);
            gbuffer.RenderTargets.DepthStencil = depthStencil;
            lightBuffer = new(device, width, height, 1, Vortice.DXGI.Format.R16G16B16A16_Float);
            fxaaBuffer = new(device, window.Width, window.Height);

            lightPipeline.ShaderResourceViews.Clear();
            lightPipeline.ShaderResourceViews.AppendRange(gbuffer, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(depthStencil.SRV, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(csmBuffer.SRV, ShaderStage.Pixel);

            compose.ShaderResourceViews.Clear();
            compose.ShaderResourceViews.Append(lightBuffer, ShaderStage.Pixel);

            fxaa.ShaderResourceViews.Clear();
            fxaa.ShaderResourceViews.Append(fxaaBuffer, ShaderStage.Pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Render(ID3D11DeviceContext context, Camera view, SceneElementCollection elements)
        {
            DebugDraw.SetCamera(view.Transform.ViewProjection);
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

            BoundingFrustum cameraFrustum = view.Transform.Frustum;

            // Depth light pass.
            csmBuffer.ClearDepth(context, 0);
            gbuffer.RenderTargets.ClearTarget(context);
            gbuffer.RenderTargets.DepthStencil.ClearDepthStencil(context);

            context.ClearState();

            for (int i = 0; i < elements.Count; i++)
            {
                /*
                GameObject element = elements[i];
                if (element is World world)
                {
                    CSMHelper.GetLightSpaceMatrices(view, directionalLight.Transform, directionalLightCB.Views, directionalLightCB.Cascades, ShadowFrustra, 5);

                    directionalLightCB.Color = directionalLight.Color;
                    directionalLightCB.Direction = directionalLight.Transform.Forward;
                    chunkDepthPrepassCSM.Update(context, directionalLightCB.Views);
                    chunkDepthPrepassCSM.BeginDraw(context);
                    context.OMSetRenderTargets((ID3D11RenderTargetView)null, csmBuffer.DSV);
                    for (int j = 0; j < world.LoadedChunks.Count; j++)
                    {
                        Chunk chunk = world.LoadedChunks[j];
                        if (chunk.VertexBuffer is not null && chunk.VertexBuffer.VertexCount != 0)
                        {
                            for (int k = 0; k < ShadowFrustra.Length; k++)
                            {
                                if (ShadowFrustra[k].Intersects(chunk.BoundingBox))
                                {
                                    chunkDepthPrepassCSM.Update(context, chunk);
                                    chunk.Bind(context);
                                    chunkDepthPrepassCSM.DrawFast(context, chunk.VertexBuffer.VertexCount, 0);
                                    break;
                                }
                            }
                        }
                    }
                }*/
            }

            context.ClearState();

            // Deferred fill GBuffers pass.
            for (int i = 0; i < elements.Count; i++)
            {
                GameObject element = elements[i];
                if (element is World world)
                {
                    chunkPrepass.BeginDraw(context);
                    gbuffer.RenderTargets.SetTarget(context);
#if USE_LEGACY_LOADER
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
#else
                    for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
                    {
                        RenderRegion region = world.LoadedRenderRegions[j];
                        if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(region.BoundingBox))
                        {
                            chunkPrepass.Update(context, view, region);
                            region.Bind(context);
                            chunkPrepass.DrawFast(context, region.VertexBuffer.VertexCount, 0);
                        }
                        DebugDraw.DrawBoundingBox(region.Name, region.BoundingBox, new(1, 1, 0, 0.8f));
                    }
                    for (int j = 0; j < world.LoadedChunkRegions.Count; j++)
                    {
                        ChunkRegion chunk = world.LoadedChunkRegions[j];
                        var min = new Vector3(chunk.Position.X, 0, chunk.Position.Y) * Chunk.CHUNK_SIZE;
                        var max = min + new Vector3(Chunk.CHUNK_SIZE) * new Vector3(1, World.CHUNK_AMOUNT_Y, 1);
                        DebugDraw.DrawBoundingBox($"{chunk.Position}+0", new(min, max), new(1, 1, 1, 0.4f));
                        ImGui.Text(chunk.Position.ToString());
                    }
#endif
                }
            }

            context.ClearState();
            lightBuffer.RenderTarget.ClearTarget(context);
            // Forward pass.
            for (int i = 0; i < elements.ForwardComponents.Count; i++)
            {
                context.OMSetRenderTargets(lightBuffer.RenderTarget.RTV, depthStencil.DSV);
                context.RSSetViewport(lightBuffer.RenderTarget.Viewport);
                IForwardRenderComponent element = elements.ForwardComponents[i];
                element.DrawForward(context, view);
            }

            // light pass
            lightPipeline.Update(context, new(view, new(width, height)), directionalLightCB);
            lightBuffer.RenderTarget.SetTarget(context);
            rectangle.DrawAuto(context, lightPipeline);

            context.ClearState();

            fxaaBuffer.RenderTarget.ClearAndSetTarget(context);
            rectangle.DrawAuto(context, compose);
            context.ClearState();

            swapChain.ClearAndSetTarget(context);
            rectangle.DrawAuto(context, fxaa);
            swapChain.DepthStencil = depthStencil;

            context.ClearState();

            swapChain.DepthStencil = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            chunkPrepass.Dispose();
            chunkDepthPrepassCSM.Dispose();
            lightPipeline.Dispose();
            fxaa.Dispose();
            compose.Dispose();
            rectangle.Dispose();
            anisotropicClamp.Dispose();
            linearClamp.Dispose();
            depthStencil.Dispose();
            gbuffer.Dispose();
            fxaaBuffer.Dispose();
            lightBuffer.Dispose();
            csmBuffer.Dispose();
        }
    }
}