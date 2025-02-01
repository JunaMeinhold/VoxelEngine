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
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class DirectionalLight : Light
    {
        public new CameraTransform Transform = new();

        public DirectionalLight()
        {
            base.Transform = Transform;
            for (int i = 0; i < 8; i++)
            {
                ShadowFrustra[i] = new();
            }
        }

        public override LightType Type => LightType.Directional;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[8];
        public CBDirectionalLightSD DirectionalLightShadowData = new();
        public int CascadeCount = 4;
        public Texture2D ShadowMap;
        public DepthStencil DepthStencil;

        public void Create()
        {
            ShadowMap = new(Format.R32G32Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, CascadeCount - 1, gpuAccessFlags: GpuAccessFlags.RW);
            ShadowMap.CreateArraySlices();
            DepthStencil = new(Format.D32Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize, CascadeCount - 1);
        }

        public unsafe void Update(GraphicsContext context, Camera camera, ConstantBuffer<CSMBuffer> csmMatrixBuffer)
        {
            CBDirectionalLightSD d = DirectionalLightShadowData;
            Matrix4x4* views = CBDirectionalLightSD.GetViews(&d);
            float* cascades = CBDirectionalLightSD.GetCascades(&d);

            CSMConfig config = new()
            {
                CascadeCount = CascadeCount,
                ShadowMapSize = Nucleus.Settings.ShadowMapSize,
            };

            CSMHelper.GetLightSpaceMatrices(camera, Transform, views, cascades, ShadowFrustra, config);
            DirectionalLightShadowData = d;
            DirectionalLightShadowData.Color = Color;
            DirectionalLightShadowData.Direction = Transform.Forward;
            DirectionalLightShadowData.CastShadows = CastShadows ? 1 : 0;
            DirectionalLightShadowData.CascadeCount = (uint)(CascadeCount - 1);

            CSMBuffer buffer = new(views, (uint)(CascadeCount - 1));

            csmMatrixBuffer.Update(context, buffer);
        }

        public unsafe void PrepareDraw(GraphicsContext context)
        {
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