using VoxelEngine.Graphics.Shaders;

namespace App.Renderers.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using App.Pipelines.Forward;
    using HexaEngine.ImGuiNET;
    using Vortice.Direct3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Primitives;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Mathematics.Sky;
    using VoxelEngine.Mathematics.Sky.HosekWilkie;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Scenes;
    using ShaderStage = ShaderStage;

    public class SkyboxRenderer : IForwardRenderComponent
    {
        private GameObject sceneElement;
        private SkyboxPipeline pipeline;

        private ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private ConstantBuffer<Colors> constantBuffer;

        private ID3D11SamplerState samplerState;

        public Texture2D Texture;
        public UVSphere sphere;
        public string TexturePath;
        private Colors colors = new();

        private float turbidity = 3;
        private float groundAlbedo = 0.1f;

        public static Vector3 SunDir;

        private struct Colors
        {
            public Vector4 LightDir;
            public Vector3 A;

            public float _paddA;
            public Vector3 B;
            public float _paddB;
            public Vector3 C;
            public float _paddC;
            public Vector3 D;
            public float _paddD;
            public Vector3 E;
            public float _paddE;
            public Vector3 F;
            public float _paddF;
            public Vector3 G;
            public float _paddG;
            public Vector3 H;
            public float _paddH;
            public Vector3 I;
            public float _paddI;
            public Vector3 Z;
            public float _paddZ;

            public Colors()
            {
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, GameObject element)
        {
            sceneElement = element;
            pipeline = new(device);
            sphere = new();
            Texture = new(device, TexturePath);

            mvpBuffer = new(device, CpuAccessFlags.Write);
            constantBuffer = new(device, CpuAccessFlags.Write);
            samplerState = device.CreateSamplerState(SamplerDescription.LinearClamp);
            pipeline.SamplerStates.Add(samplerState, ShaderStage.Pixel, 0);
            pipeline.ShaderResourceViews.Add(Texture.SRV, ShaderStage.Pixel, 0);
            pipeline.ConstantBuffers.Add(mvpBuffer, ShaderStage.Vertex, 0);
            pipeline.ConstantBuffers.Add(constantBuffer, ShaderStage.Pixel, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ID3D11DeviceContext context, IView view)
        {
            Vector3 sunDir = Vector3.Normalize(-SunDir);
            SkyParameters skyParams = SkyModel.CalculateSkyParameters(turbidity, groundAlbedo, sunDir);
            colors.A = skyParams[(int)EnumSkyParams.A];
            colors.B = skyParams[(int)EnumSkyParams.B];
            colors.C = skyParams[(int)EnumSkyParams.C];
            colors.D = skyParams[(int)EnumSkyParams.D];
            colors.E = skyParams[(int)EnumSkyParams.E];
            colors.F = skyParams[(int)EnumSkyParams.F];
            colors.G = skyParams[(int)EnumSkyParams.G];
            colors.H = skyParams[(int)EnumSkyParams.H];
            colors.I = skyParams[(int)EnumSkyParams.I];
            colors.Z = skyParams[(int)EnumSkyParams.Z];
            colors.LightDir = new(sunDir, 1);
            if (ImGui.Begin("Sky"))
            {
                ImGui.Text(sunDir.ToString());
                ImGui.InputFloat("Turbidity", ref turbidity);
                ImGui.InputFloat("GroundAlbedo", ref groundAlbedo);
            }
            ImGui.End();

            constantBuffer.Update(context, colors);
            mvpBuffer.Update(context, new ModelViewProjBuffer(view, Matrix4x4.CreateScale(view.Transform.Far) * Matrix4x4.CreateTranslation(view.Transform.Position)));
            sphere.Bind(context);
            pipeline.Begin(context);
            context.DrawIndexed(sphere.IndexBuffer.Count, 0, 0);
            pipeline.End(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Uninitialize()
        {
            pipeline.Dispose();
            pipeline = null;
            constantBuffer.Dispose();
            sphere.Dispose();
            Texture.Dispose();
            Texture = null;
            sceneElement = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoundingBox GetBoundingBox()
        {
            return new BoundingBox(new Vector3(float.NaN), new Vector3(float.NaN));
        }
    }
}