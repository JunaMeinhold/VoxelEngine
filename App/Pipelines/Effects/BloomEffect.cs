﻿namespace App.Pipelines.Effects
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;

    public unsafe class BloomEffect : DisposableBase
    {
        private readonly GraphicsPipelineState downsample;
        private readonly GraphicsPipelineState upsample;
        private readonly ConstantBuffer<ParamsDownsample> downsampleCB;
        private readonly ConstantBuffer<ParamsUpsample> upsampleCB;
        private readonly SamplerState sampler;

        private Texture2D[] textures;
        private Hexa.NET.Mathematics.Viewport[] viewports;

        private readonly float radius = 0.003f;
        private int width;
        private int height;
        private bool dirty;

        public BloomEffect(int width, int height)
        {
            downsampleCB = new(CpuAccessFlag.Write);
            upsampleCB = new(CpuAccessFlag.Write);

            sampler = new(SamplerDescription.LinearClamp);

            downsample = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "bloom/downsample/ps.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);

            downsample.Bindings.SetCBV("Params", downsampleCB);
            downsample.Bindings.SetSampler("samplerState", sampler);

            upsample = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "bloom/upsample/ps.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);

            upsample.Bindings.SetCBV("Params", upsampleCB);
            upsample.Bindings.SetSampler("samplerState", sampler);

            int currentWidth = width / 2;
            int currentHeight = height / 2;
            int levels = Math.Min(TextureHelper.ComputeMipLevels(currentWidth, currentHeight), 8);

            textures = new Texture2D[levels];
            viewports = new Hexa.NET.Mathematics.Viewport[levels];
            for (int i = 0; i < levels; i++)
            {
                textures[i] = new(Format.R16G16B16A16Float, currentWidth, currentHeight, 1, 1, gpuAccessFlags: GpuAccessFlags.RW);
                viewports[i] = new(currentWidth, currentHeight);
                currentWidth /= 2;
                currentHeight /= 2;
            }

            this.width = width;
            this.height = height;

            dirty = true;
        }

        public ShaderResourceView Output => textures[0].SRV;

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

            textures = new Texture2D[levels];
            viewports = new Hexa.NET.Mathematics.Viewport[levels];

            for (int i = 0; i < levels; i++)
            {
                textures[i] = new(Format.R16G16B16A16Float, currentWidth, currentHeight, 1, 1, gpuAccessFlags: GpuAccessFlags.RW);
                currentWidth /= 2;
                currentHeight /= 2;
            }

            this.width = width;
            this.height = height;
            dirty = true;
        }

        public void Update(ComPtr<ID3D11DeviceContext> context)
        {
            if (dirty)
            {
                Vector4 col = default;
                context.ClearRenderTargetView(textures[0].RTV, (float*)&col);
                downsampleCB.Update(context, new ParamsDownsample(new(width, height)));
                upsampleCB.Update(context, new ParamsUpsample(radius));
                dirty = false;
            }
        }

        public void Pass(ComPtr<ID3D11DeviceContext> context, IShaderResourceView input)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                context.SetRenderTarget(textures[i], null);
                if (i > 0)
                {
                    downsample.Bindings.SetSRV("srcTexture", textures[i - 1]);
                }
                else
                {
                    downsample.Bindings.SetSRV("srcTexture", input);
                }

                downsample.Begin(context);
                context.RSSetViewport(viewports[i]);
                context.DrawInstanced(4, 1, 0, 0);
                downsample.End(context);
                context.SetRenderTarget(null, null);
            }

            for (int i = textures.Length - 1; i > 0; i--)
            {
                context.SetRenderTarget(textures[i - 1], null);
                upsample.Bindings.SetSRV("srcTexture", textures[i]);
                upsample.Begin(context);
                context.RSSetViewport(viewports[i - 1]);
                context.DrawInstanced(4, 1, 0, 0);
                upsample.End(context);
                context.SetRenderTarget(null, null);
            }
        }

        protected override void DisposeCore()
        {
            downsample.Dispose();
            upsample.Dispose();
            downsampleCB.Dispose();
            upsampleCB.Dispose();
            sampler.Dispose();
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Dispose();
            }
        }
    }
}