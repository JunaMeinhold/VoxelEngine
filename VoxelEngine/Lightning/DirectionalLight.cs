namespace VoxelEngine.Lightning
{
    using Hexa.NET.D3D11;
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using System;
    using System.Numerics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class DirectionalLight : Light
    {
        public new CameraTransform Transform = new();

        public DirectionalLight()
        {
            base.Transform = Transform;
            for (int i = 0; i < 16; i++)
            {
                ShadowFrustra[i] = new();
            }
        }

        public override LightType Type => LightType.Directional;

        public BoundingFrustum[] ShadowFrustra = new BoundingFrustum[16];
        public CBDirectionalLightSD DirectionalLightShadowData = new();

        public unsafe void Update(ComPtr<ID3D11DeviceContext> context, Camera camera, ConstantBuffer<Matrix4x4> csmMatrixBuffer)
        {
            CBDirectionalLightSD d = DirectionalLightShadowData;
            Matrix4x4* views = CBDirectionalLightSD.GetViews(&d);
            float* cascades = CBDirectionalLightSD.GetCascades(&d);
            CSMHelper.GetLightSpaceMatrices(camera, Transform, views, cascades, ShadowFrustra, 5);
            DirectionalLightShadowData = d;
            DirectionalLightShadowData.Color = Color;
            DirectionalLightShadowData.Direction = Transform.Forward;
            DirectionalLightShadowData.CastShadows = CastShadows ? 1 : 0;
            csmMatrixBuffer.Update(context, views, 8);
        }
    }
}