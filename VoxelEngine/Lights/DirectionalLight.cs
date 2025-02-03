namespace VoxelEngine.Lightning
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using System;
    using System.Numerics;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lights;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class DirectionalLight : Light
    {
        public new CameraTransform Transform = new();

        public DirectionalLight()
        {
            OverwriteTransform(Transform);
            for (int i = 0; i < 8; i++)
            {
                ShadowFrustra[i] = new();
            }
        }

        public override LightType Type => LightType.Directional;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[8];

        public int CascadeCount = 4;
        public Texture2D? ShadowMap;
        public DepthStencil? DepthStencil;
        public int Size = Nucleus.Settings.ShadowMapSize;
        public float LightBleedingReduction;
        private ShadowData data;

        public override bool HasShadowMap => ShadowMap != null;

        public override void Awake()
        {
            base.Awake();
        }

        public override void CreateShadowMap()
        {
            if (ShadowMap != null) return;
            ShadowMap = new(Format.R32G32Float, Size, Size, CascadeCount - 1, gpuAccessFlags: GpuAccessFlags.RW);
            ShadowMap.CreateArraySlices();
            DepthStencil = new(Format.D32Float, Size, Size, CascadeCount - 1);
        }

        public override void DestroyShadowMap()
        {
            if (ShadowMap == null || DepthStencil == null) return;
            ShadowMap.Dispose();
            ShadowMap = null;
            DepthStencil.Dispose();
            DepthStencil = null;
        }

        public unsafe void Update(GraphicsContext context, ConstantBuffer<CSMBuffer> csmMatrixBuffer)
        {
            ShadowData shadow = data;
            Matrix4x4* views = ShadowData.GetViews(&shadow);
            CSMBuffer buffer = new(views, (uint)(CascadeCount - 1));
            csmMatrixBuffer.Update(context, buffer);
        }

        public override unsafe void Update(GraphicsContext context, Camera camera, StructuredBuffer<LightData> lightBuffer, StructuredBuffer<ShadowData> shadowDataBuffer)
        {
            lightBuffer.Add(new(this));
            if (!CastShadows) return;
            ShadowData* shadow = &shadowDataBuffer.Items[ShadowMapIndex];
            shadow->Size = Size;
            shadow->CascadeCount = (uint)(CascadeCount - 1);
            shadow->Softness = LightBleedingReduction;

            Matrix4x4* views = ShadowData.GetViews(shadow);
            float* cascades = ShadowData.GetCascades(shadow);

            CSMConfig config = new()
            {
                CascadeCount = CascadeCount,
                ShadowMapSize = Size,
                Stabilize = true,
            };

            CSMHelper.GetLightSpaceMatrices(camera, Transform, views, cascades, ShadowFrustra, config);

            data = *shadow;
        }

        public unsafe void PrepareDraw(GraphicsContext context)
        {
            if (ShadowMap == null || DepthStencil == null) return;
            context.ClearDepthStencilView(DepthStencil, ClearFlag.Depth, 1, 0);
            context.ClearRenderTargetView(ShadowMap, default);
            context.SetViewport(ShadowMap.Viewport);
            context.SetRenderTarget(ShadowMap, DepthStencil);
        }

        public void Dispose()
        {
            ShadowMap.Dispose();
            DepthStencil.Dispose();
        }
    }
}