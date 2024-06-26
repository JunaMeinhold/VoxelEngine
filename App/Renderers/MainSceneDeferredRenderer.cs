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
    using App.Renderers.Forward;
    using HexaEngine.Editor;
    using Hexa.NET.ImGui;
    using Vortice.Direct3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Lightning;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.DXGI;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using VoxelEngine.Windows;

    public class MainSceneDeferredRenderer : ISceneRenderer
    {
        private ConstantBuffer<CBCamera> camera;

        private SwapChain swapChain;
        private ChunkGeometryPipeline chunkPrepass;
        private CSMChunkPipeline chunkDepthPrepassCSM;
        private LightPipeline lightPipeline;
        private ComposeEffect compose;
        private FXAAEffect fxaa;
        private HBAOEffect hbao;
        private GodRaysEffect godRays;
        private BloomEffect bloom;

        private ID3D11SamplerState anisotropicClamp;
        private ID3D11SamplerState pointClamp;
        private ID3D11SamplerState linearClamp;

        private DepthStencil depthStencil;
        private GBuffer gbuffer;
        private Texture2D lightBuffer;
        private Texture2D fxaaBuffer;

        private Texture2D hbaoBuffer;
        private DepthStencil csmBuffer;

        private PerlinNoiseWidget perlinNoiseWidget;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[16];

        private DirectionalLight directionalLight;
        private CBDirectionalLightSD directionalLightCB = new();
        private int width;
        private int height;

        private bool debugChunksRegion;

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

            camera = new(device, CpuAccessFlags.Write);

            depthStencil = new(device, window.Width, window.Height);
            gbuffer = new(device, window.Width, window.Height, Vortice.DXGI.Format.R16G16B16A16_Float, Vortice.DXGI.Format.R8G8B8A8_UNorm, Vortice.DXGI.Format.R16G16B16A16_Float, Vortice.DXGI.Format.R16G16B16A16_Float);
            gbuffer.RenderTargets.DepthStencil = depthStencil;
            lightBuffer = new(device, Vortice.DXGI.Format.R16G16B16A16_Float, width, height, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);
            fxaaBuffer = new(device, Vortice.DXGI.Format.R16G16B16A16_Float, window.Width, window.Height, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);
            csmBuffer = new(device, Vortice.DXGI.Format.D32_Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, 5);
            hbaoBuffer = new(device, Vortice.DXGI.Format.R32_Float, width, height, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);

            lightPipeline = new(device);
            lightPipeline.ShaderResourceViews.AppendRange(gbuffer, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(depthStencil.SRV, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(csmBuffer.SRV, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(hbaoBuffer.SRV, ShaderStage.Pixel);
            lightPipeline.SamplerStates.Add(linearClamp, ShaderStage.Pixel, 0);
            lightPipeline.SamplerStates.Add(linearClamp, ShaderStage.Pixel, 4);

            bloom = new(device, width, height);

            compose = new(device);
            compose.Input = lightBuffer;
            compose.Bloom = bloom.Output;
            compose.Depth = depthStencil.SRV;
            compose.SamplerStates.Append(linearClamp, ShaderStage.Pixel);
            compose.Camera = camera;

            fxaa = new(device);
            fxaa.Input = fxaaBuffer;

            hbao = new(device);
            hbao.Depth = depthStencil.SRV;
            hbao.Normal = gbuffer.SRVs[1];

            godRays = new(device, width, height);

            chunkPrepass = new(device);
            chunkDepthPrepassCSM = new(device);

            directionalLight = new();
            directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 1.4f;
            directionalLight.Transform.Far = 100;
            directionalLight.Transform.Rotation = new(0, 100, 0);
            directionalLight.CastShadows = true;
            Keyboard.KeyUp += Keyboard_OnKeyUp;

            perlinNoiseWidget = new(device);
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
            if (e.KeyCode == Key.F5)
            {
                debugChunksRegion = !debugChunksRegion;
            }
        }

        public void Resize(ID3D11Device device, GameWindow window)
        {
            width = window.Width;
            height = window.Height;

            depthStencil.Resize(device, width, height);
            gbuffer.Resize(device, width, height);
            gbuffer.RenderTargets.DepthStencil = depthStencil;
            lightBuffer.Resize(device, width, height);
            fxaaBuffer.Resize(device, width, height);
            hbaoBuffer.Resize(device, width, height);

            hbao.Depth = depthStencil.SRV;
            hbao.Normal = gbuffer.SRVs[1];

            godRays.Resize(width, height);

            lightPipeline.ShaderResourceViews.Clear();
            lightPipeline.ShaderResourceViews.AppendRange(gbuffer, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(depthStencil.SRV, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(csmBuffer.SRV, ShaderStage.Pixel);
            lightPipeline.ShaderResourceViews.Append(hbaoBuffer.SRV, ShaderStage.Pixel);

            bloom.Resize(width, height);

            compose.Input = lightBuffer;
            compose.Bloom = bloom.Output;
            compose.Depth = depthStencil.SRV;

            fxaa.Input = fxaaBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Render(ID3D11DeviceContext context, Camera view, SceneElementCollection elements)
        {
            perlinNoiseWidget.Draw(context);

            ImGui.Text($"{1 / Time.Delta} FPS / {Time.Delta * 1000}ms");
            ImGui.InputFloat("TimeScale", ref Time.TimeScale);
            float gt = Time.GameTime;
            if (ImGui.InputFloat("GameTime", ref gt))
            {
                Time.GameTime = gt;
            }

            float fogStart = compose.FogStart;
            if (ImGui.InputFloat("FogStart", ref fogStart))
            {
                compose.FogStart = fogStart;
            }

            float fogEnd = compose.FogEnd;
            if (ImGui.InputFloat("FogEnd", ref fogEnd))
            {
                compose.FogEnd = fogEnd;
            }

            DebugDraw.SetCamera(view.Transform.ViewProjection);
            Vector3 rot = directionalLight.Transform.Rotation;
            rot.Y = 360 * Time.GameTimeNormalized - 90;
            float ro = (rot.Y - 180F).NormalizeEulerAngleDegrees();
            directionalLight.Transform.Rotation = rot.NormalizeEulerAngleDegrees();
            if (rot.Y > 45 && rot.Y < 135)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1);
            }
            else if (rot.Y > 135 && rot.Y < 225)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(1, 0.2f, (rot.Y - 135) / 90);
            }
            else if (ro > 45 && ro < 135)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * 0.2f;
            }
            else if (ro > 135 && ro < 225)
            {
                directionalLight.Color = new Vector4(196 / 255f, 220 / 255f, 1, 1) * MathUtil.Lerp(0.2f, 1, (ro - 135) / 90);
            }
            directionalLight.Color *= 5;
            directionalLight.Transform.Recalculate();

            BoundingFrustum cameraFrustum = view.Transform.Frustum;

            camera.Update(context, new CBCamera(view, new Vector2(width, height)));

            // Depth light pass.
            context.ClearDepthStencilView(csmBuffer, DepthStencilClearFlags.Depth, 1, 0);
            gbuffer.RenderTargets.ClearTarget(context);
            context.ClearDepthStencilView(gbuffer.RenderTargets.DepthStencil, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);

            context.ClearState();

            for (int i = 0; i < elements.Count; i++)
            {
                GameObject element = elements[i];
                if (element is World world)
                {
                    CBDirectionalLightSD d = directionalLightCB;
                    Matrix4x4* views = CBDirectionalLightSD.GetViews(&d);
                    float* cascades = CBDirectionalLightSD.GetCascades(&d);
                    CSMHelper.GetLightSpaceMatrices(view, directionalLight.Transform, views, cascades, ShadowFrustra, 5);
                    directionalLightCB = d;
                    directionalLightCB.Color = directionalLight.Color;
                    SkyboxRenderer.SunDir = directionalLightCB.Direction = directionalLight.Transform.Forward;
                    directionalLightCB.CastShadows = directionalLight.CastShadows ? 1 : 0;
                    chunkDepthPrepassCSM.Update(context, views);
                    chunkDepthPrepassCSM.Begin(context);
                    context.RSSetViewport(csmBuffer.Viewport);
                    context.OMSetRenderTargets((ID3D11RenderTargetView)null, csmBuffer.DSV);

                    for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
                    {
                        RenderRegion region = world.LoadedRenderRegions[j];
                        if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(region.BoundingBox))
                        {
                            chunkDepthPrepassCSM.Update(context);
                            region.Bind(context);
                            context.Draw(region.VertexBuffer.VertexCount, 0);
                        }
                    }
                }
            }

            context.ClearState();

            // Deferred fill GBuffers pass.
            for (int i = 0; i < elements.Count; i++)
            {
                GameObject element = elements[i];
                if (element is World world)
                {
                    chunkPrepass.Begin(context);
                    gbuffer.RenderTargets.SetTarget(context);

                    for (int j = 0; j < world.LoadedRenderRegions.Count; j++)
                    {
                        RenderRegion region = world.LoadedRenderRegions[j];
                        if (region.VertexBuffer is not null && region.VertexBuffer.VertexCount != 0 && cameraFrustum.Intersects(region.BoundingBox))
                        {
                            chunkPrepass.Update(context, view);
                            region.Bind(context);
                            context.Draw(region.VertexBuffer.VertexCount, 0);
                        }
                        if (debugChunksRegion)
                        {
                            DebugDraw.DrawBoundingBox(region.Name, region.BoundingBox, new(1, 1, 0, 0.8f));
                        }
                    }
                    if (debugChunksRegion)
                    {
                        for (int j = 0; j < world.LoadedChunkSegments.Count; j++)
                        {
                            ChunkSegment chunk = world.LoadedChunkSegments[j];
                            Vector3 min = new Vector3(chunk.Position.X, 0, chunk.Position.Y) * Chunk.CHUNK_SIZE;
                            Vector3 max = min + new Vector3(Chunk.CHUNK_SIZE) * new Vector3(1, WorldMap.CHUNK_AMOUNT_Y, 1);
                            DebugDraw.DrawBoundingBox($"{chunk.Position}+0", new(min, max), new(1, 1, 1, 0.4f));
                        }
                    }
                }
            }

            hbao.Update(context, view, hbaoBuffer.Viewport);
            context.OMSetRenderTargets(hbaoBuffer);
            context.RSSetViewport(hbaoBuffer.Viewport);
            hbao.Pass(context);

            godRays.Update(context, view, directionalLight);
            godRays.PrePass(context, depthStencil);

            context.ClearState();
            context.ClearRenderTargetView(lightBuffer, default);

            // Forward pass.
            for (int i = 0; i < elements.ForwardComponents.Count; i++)
            {
                context.OMSetRenderTargets(lightBuffer.RTV, depthStencil.DSV);
                context.RSSetViewport(lightBuffer.Viewport);
                IForwardRenderComponent element = elements.ForwardComponents[i];

                element.DrawForward(context, view);
            }

            // light pass
            lightPipeline.Update(context, new(view, new Vector2(width, height)), directionalLightCB);
            context.OMSetRenderTargets(lightBuffer);
            context.RSSetViewport(lightBuffer.Viewport);
            lightPipeline.Pass(context);

            bloom.Update(context);
            bloom.Pass(context, lightBuffer.SRV);

            context.OMSetRenderTargets(lightBuffer);
            context.RSSetViewport(lightBuffer.Viewport);
            godRays.Pass(context);

            context.ClearRenderTargetView(fxaaBuffer, default);
            context.OMSetRenderTargets(fxaaBuffer);
            context.RSSetViewport(fxaaBuffer.Viewport);
            compose.Pass(context);

            swapChain.DepthStencil = null;
            swapChain.ClearAndSetTarget(context);
            fxaa.Pass(context);
            swapChain.DepthStencil = depthStencil;

            context.ClearState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            camera.Dispose();
            chunkPrepass.Dispose();
            chunkDepthPrepassCSM.Dispose();
            lightPipeline.Dispose();
            fxaa.Dispose();
            hbao.Dispose();
            compose.Dispose();
            anisotropicClamp.Dispose();
            linearClamp.Dispose();
            depthStencil.Dispose();
            gbuffer.Dispose();
            fxaaBuffer.Dispose();
            hbaoBuffer.Dispose();
            lightBuffer.Dispose();
            csmBuffer.Dispose();

            perlinNoiseWidget.Release();
        }
    }
}