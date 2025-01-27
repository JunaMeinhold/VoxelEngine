namespace App.Pipelines.Effects
{
    using System.Numerics;
    using Hexa.NET.Mathematics;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Scenes;
    using Viewport = Vortice.Mathematics.Viewport;

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

                Free(pixelData);
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
                hbaoParams.SamplingRadiusToScreen = samplingRadius * 0.5f * viewport.Height / (MathF.Tan(MathUtil.ToRad(camera.Fov) * 0.5f) * 2.0f); ;
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