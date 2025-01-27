namespace App.Pipelines.Effects
{
    using System.Numerics;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.Shaders;

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
}