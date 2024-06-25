namespace App.Pipelines.Effects
{
    using System.Numerics;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Scenes;

    public class BloomEffect
    {
        private GraphicsPipeline downsample;
        private GraphicsPipeline upsample;
        private ConstantBuffer<ParamsDownsample> downsampleCB;
        private ConstantBuffer<ParamsUpsample> upsampleCB;
        private ID3D11SamplerState sampler;

        private ID3D11Texture2D[] textures;
        private ID3D11RenderTargetView[] mipChainRTVs;
        private ID3D11ShaderResourceView[] mipChainSRVs;
        private Viewport[] viewports;

        private float radius = 0.003f;
        private readonly ID3D11Device device;
        private int width;
        private int height;
        private bool dirty;
        private bool disposedValue;

        public BloomEffect(ID3D11Device device, int width, int height)
        {
            downsampleCB = new(device, CpuAccessFlags.Write);
            upsampleCB = new(device, CpuAccessFlags.Write);

            sampler = device.CreateSamplerState(SamplerDescription.LinearClamp);

            downsample = new(device, new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "bloom/downsample/ps.hlsl",
            }, GraphicsPipelineState.DefaultFullscreen);
            upsample = new(device, new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "bloom/upsample/ps.hlsl",
            }, GraphicsPipelineState.DefaultFullscreen);

            int currentWidth = width / 2;
            int currentHeight = height / 2;
            int levels = Math.Min(TextureHelper.ComputeMipLevels(currentWidth, currentHeight), 8);

            textures = new ID3D11Texture2D[levels];
            mipChainRTVs = new ID3D11RenderTargetView[levels];
            mipChainSRVs = new ID3D11ShaderResourceView[levels];
            viewports = new Viewport[levels];
            for (int i = 0; i < levels; i++)
            {
                textures[i] = device.CreateTexture2D(new(Format.R16G16B16A16_Float, currentWidth, currentHeight, 1, 1, BindFlags.ShaderResource | BindFlags.RenderTarget));
                mipChainRTVs[i] = device.CreateRenderTargetView(textures[i]);
                mipChainSRVs[i] = device.CreateShaderResourceView(textures[i]);
                viewports[i] = new(currentWidth, currentHeight);
                currentWidth /= 2;
                currentHeight /= 2;
            }

            this.device = device;
            this.width = width;
            this.height = height;

            dirty = true;
        }

        public ID3D11ShaderResourceView Output => mipChainSRVs[0];

        #region Structs

        private struct ParamsDownsample
        {
            public Vector2 SrcResolution;
            public Vector2 Padd;

            public ParamsDownsample(Vector2 srcResolution)
            {
                SrcResolution = srcResolution;
                Padd = default;
            }
        }

        private struct ParamsUpsample
        {
            public float FilterRadius;
            public Vector3 Padd;

            public ParamsUpsample(float filterRadius)
            {
                FilterRadius = filterRadius;
                Padd = default;
            }
        }

        #endregion Structs

        public void Resize(int width, int height)
        {
            int currentWidth = width / 2;
            int currentHeight = height / 2;
            int levels = Math.Min(TextureHelper.ComputeMipLevels(currentWidth, currentHeight), 8);

            textures = new ID3D11Texture2D[levels];
            mipChainRTVs = new ID3D11RenderTargetView[levels];
            mipChainSRVs = new ID3D11ShaderResourceView[levels];
            viewports = new Viewport[levels];

            for (int i = 0; i < levels; i++)
            {
                textures[i] = device.CreateTexture2D(new(Format.R16G16B16A16_Float, currentWidth, currentHeight, 1, 1, BindFlags.ShaderResource | BindFlags.RenderTarget));
                mipChainRTVs[i] = device.CreateRenderTargetView(textures[i]);
                mipChainSRVs[i] = device.CreateShaderResourceView(textures[i]);
                currentWidth /= 2;
                currentHeight /= 2;
            }

            this.width = width;
            this.height = height;
            dirty = true;
        }

        public void Update(ID3D11DeviceContext context)
        {
            if (dirty)
            {
                context.ClearRenderTargetView(mipChainRTVs[0], default);
                downsampleCB.Update(context, new ParamsDownsample(new(width, height)));
                upsampleCB.Update(context, new ParamsUpsample(radius));
                dirty = false;
            }
        }

        public void Pass(ID3D11DeviceContext context, ID3D11ShaderResourceView input)
        {
            context.ClearState();
            context.PSSetConstantBuffer(0, downsampleCB);
            context.PSSetSampler(0, sampler);
            downsample.Begin(context);
            for (int i = 0; i < mipChainRTVs.Length; i++)
            {
                if (i > 0)
                {
                    context.PSSetShaderResource(0, mipChainSRVs[i - 1]);
                }
                else
                {
                    context.PSSetShaderResource(0, input);
                }

                context.OMSetRenderTargets(mipChainRTVs[i], null);
                context.RSSetViewport(viewports[i]);
                context.DrawInstanced(4, 1, 0, 0);
                context.PSSetShaderResource(0, null);
                context.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
            }

            context.PSSetConstantBuffer(0, upsampleCB);
            upsample.Begin(context);
            for (int i = mipChainRTVs.Length - 1; i > 0; i--)
            {
                context.OMSetRenderTargets(mipChainRTVs[i - 1], null);
                context.PSSetShaderResource(0, mipChainSRVs[i]);
                context.RSSetViewport(viewports[i - 1]);
                context.DrawInstanced(4, 1, 0, 0);
                context.PSSetShaderResource(0, null);
                context.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
            }
            context.PSSetSampler(0, null);
            context.PSSetConstantBuffer(0, null);
            upsample.End(context);
            context.ClearState();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                downsample.Dispose();
                upsample.Dispose();
                downsampleCB.Dispose();
                upsampleCB.Dispose();
                sampler.Dispose();
                for (int i = 0; i < mipChainRTVs.Length; i++)
                {
                    textures[i].Dispose();
                    mipChainRTVs[i].Dispose();
                    mipChainSRVs[i].Dispose();
                }

                disposedValue = true;
            }
        }
    }

    public class HBAOEffect
    {
        private readonly GraphicsPipeline pipeline;
        private readonly ConstantBuffer<CBCamera> cameraBuffer;
        private readonly ConstantBuffer<HBAOParams> paramsBuffer;
        private readonly Texture2D noiseTex;

        private readonly ID3D11SamplerState samplerState;

        private readonly float samplingRadius = 0.5f;
        private readonly uint numSamplingDirections = 8;
        private readonly float samplingStep = 0.004f;
        private readonly uint numSamplingSteps = 4;
        private readonly float power = 1;
        private readonly int priority;
        private bool isDirty = true;
        private const int NoiseSize = 4;
        private const int NoiseStride = 4;

        private struct HBAOParams
        {
            public float SamplingRadius;
            public float SamplingRadiusToScreen;
            public uint NumSamplingDirections;
            public float SamplingStep;

            public uint NumSamplingSteps;
            public float Power;
            public Vector2 NoiseScale;

            public HBAOParams()
            {
                SamplingRadius = 0.5f;
                SamplingRadiusToScreen = 0;
                NumSamplingDirections = 8;
                SamplingStep = 0.004f;
                NumSamplingSteps = 4;
                Power = 1;
            }
        }

        public HBAOEffect(ID3D11Device device)
        {
            pipeline = new(device, new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "hbao/ps.hlsl",
            }, GraphicsPipelineState.DefaultFullscreen);
            paramsBuffer = new(device, CpuAccessFlags.Write);
            cameraBuffer = new(device, CpuAccessFlags.Write);
            unsafe
            {
                Texture2DDescription description = new(Format.R32G32B32A32_Float, NoiseSize, NoiseSize, 1, 1, BindFlags.ShaderResource, ResourceUsage.Immutable);

                float* pixelData = AllocT<float>(NoiseSize * NoiseSize * NoiseStride);

                SubresourceData initialData = default;
                initialData.DataPointer = (nint)pixelData;
                initialData.RowPitch = NoiseSize * NoiseStride;

                int idx = 0;
                for (int i = 0; i < NoiseSize * NoiseSize; i++)
                {
                    float rand = Random.Shared.NextSingle() * float.Pi * 2.0f;
                    pixelData[idx++] = MathF.Sin(rand);
                    pixelData[idx++] = MathF.Cos(rand);
                    pixelData[idx++] = Random.Shared.NextSingle();
                    pixelData[idx++] = 1.0f;
                }

                noiseTex = new(device, description, initialData);
            }

            samplerState = device.CreateSamplerState(SamplerDescription.LinearClamp);

            pipeline.ConstantBuffers.Add(paramsBuffer, ShaderStage.Pixel, 0);
            pipeline.ConstantBuffers.Add(cameraBuffer, ShaderStage.Pixel, 1);
            pipeline.ShaderResourceViews.Add(noiseTex.SRV, ShaderStage.Pixel, 2);
            pipeline.SamplerStates.Add(samplerState, ShaderStage.Pixel, 0);
        }

        public ID3D11ShaderResourceView Depth { set => pipeline.ShaderResourceViews.SetOrAdd(value, ShaderStage.Pixel, 0); }

        public ID3D11ShaderResourceView Normal { set => pipeline.ShaderResourceViews.SetOrAdd(value, ShaderStage.Pixel, 1); }

        public void Update(ID3D11DeviceContext context, Camera camera, Viewport viewport)
        {
            cameraBuffer.Update(context, new CBCamera(camera, viewport));
            if (isDirty)
            {
                HBAOParams hbaoParams = default;
                hbaoParams.SamplingRadius = samplingRadius;
                hbaoParams.SamplingRadiusToScreen = samplingRadius * 0.5f * viewport.Height / (MathF.Tan(camera.Fov.ToRad() * 0.5f) * 2.0f); ;
                hbaoParams.SamplingStep = samplingStep;
                hbaoParams.NumSamplingSteps = numSamplingSteps;
                hbaoParams.NumSamplingDirections = numSamplingDirections;
                hbaoParams.Power = power;
                hbaoParams.NoiseScale = new Vector2(viewport.Width, viewport.Height) / NoiseSize;

                paramsBuffer.Update(context, hbaoParams);
                isDirty = false;
            }
        }

        public void Pass(ID3D11DeviceContext context)
        {
            pipeline.Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            pipeline.End(context);
        }

        public void Dispose()
        {
            pipeline.Dispose();
            cameraBuffer.Dispose();
            paramsBuffer.Dispose();
            noiseTex.Dispose();
            samplerState.Dispose();
        }
    }
}