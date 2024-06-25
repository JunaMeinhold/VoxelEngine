namespace App.Pipelines.Effects
{
    using System.Numerics;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Rendering.Shaders;

    public class ComposeEffect : GraphicsPipeline
    {
        private readonly ConstantBuffer<ComposeParams> cbOptions;
        private bool isDirty = true;
        private float fogStart = 250;
        private float fogEnd = 300;
        private Vector3 fogColor = Vector3.One;

        public ComposeEffect(ID3D11Device device) : base(device, new()
        {
            VertexShader = "quad.hlsl",
            PixelShader = "compose/ps.hlsl",
        }, GraphicsPipelineState.DefaultFullscreen)
        {
            cbOptions = new(device, CpuAccessFlags.Write);
            ConstantBuffers.Add(cbOptions, ShaderStage.Pixel, 0);
        }

        private struct ComposeParams
        {
            public float BloomStrength;
            public float FogStart;
            public float FogEnd;
            public Vector3 FogColor;
            public float LUTAmountChroma;
            public float LUTAmountLuma;

            public ComposeParams(float bloomStrength = 1, float fogStart = 0.2f, float fogEnd = 1, Vector3 fogColor = default, float lutAmountChroma = 1, float lutAmountLuma = 1)
            {
                BloomStrength = bloomStrength;
                FogStart = fogStart;
                FogEnd = fogEnd;
                FogColor = fogColor;
                LUTAmountChroma = lutAmountChroma;
                LUTAmountLuma = lutAmountLuma;
            }
        }

        public ID3D11ShaderResourceView Input { set => ShaderResourceViews.SetOrAdd(value, ShaderStage.Pixel, 0); }

        public ID3D11ShaderResourceView Bloom { set => ShaderResourceViews.SetOrAdd(value, ShaderStage.Pixel, 1); }

        public ID3D11ShaderResourceView Depth { set => ShaderResourceViews.SetOrAdd(value, ShaderStage.Pixel, 3); }

        public ID3D11Buffer Camera { set => ConstantBuffers.SetOrAdd(value, ShaderStage.Pixel, 1); }

        public float FogStart
        {
            get => fogStart;
            set
            {
                fogStart = value;
                isDirty = true;
            }
        }

        public float FogEnd
        {
            get => fogEnd;
            set
            {
                fogEnd = value;
                isDirty = true;
            }
        }

        public Vector3 FogColor
        {
            get => fogColor;
            set
            {
                fogColor = value;
                isDirty = true;
            }
        }

        public void Pass(ID3D11DeviceContext context)
        {
            if (isDirty)
            {
                ComposeParams composeParams = new(0.04f, fogStart, fogEnd, fogColor);
                cbOptions.Update(context, composeParams);
                isDirty = false;
            }
            Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            End(context);
        }

        public override void Dispose()
        {
            cbOptions.Dispose();
            base.Dispose();
        }
    }
}