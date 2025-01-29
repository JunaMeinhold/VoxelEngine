namespace App.Renderers
{
    using App.Pipelines.Forward;
    using Hexa.NET.D3D11;
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Mathematics.Sky;
    using Hexa.NET.Mathematics.Sky.Preetham;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Graphics.Primitives;
    using VoxelEngine.Scenes;

    public class SkyboxRenderer : BaseRenderComponent
    {
        private SkyboxPipeline pipeline;

        private ConstantBuffer<Matrix4x4> mvpBuffer;
        private ConstantBuffer<Colors> constantBuffer;

        private SamplerState samplerState;

        public Texture2D Texture;
        public UVSphere sphere;
        public string TexturePath;
        private Colors colors = new();

        private float turbidity = 3;
        private float groundAlbedo = 0.1f;

        public override int QueueIndex { get; } = (int)RenderQueueIndex.Background;

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
        public override void Awake()
        {
            pipeline = new();
            sphere = new();
            Texture = new(TexturePath);

            mvpBuffer = new(CpuAccessFlag.Write);
            constantBuffer = new(CpuAccessFlag.Write);
            pipeline.Bindings.SetSRV("skyTexture", Texture);
            pipeline.Bindings.SetCBV("ModelBuffer", mvpBuffer);
            pipeline.Bindings.SetCBV("WeatherCBuf", constantBuffer);
        }

        public override void Draw(ComPtr<ID3D11DeviceContext> context, PassIdentifer pass, Camera camera, object? parameter)
        {
            if (pass == PassIdentifer.ForwardPass)
            {
                DrawForward(context);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawForward(ComPtr<ID3D11DeviceContext> context)
        {
            Camera camera = Camera.Current;
            Vector3 sunDir = Vector3.Normalize(-SunDir);
            SkyParameters skyParams = SkyModel.CalculateSkyParameters(turbidity, sunDir, 0, 0);
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
            mvpBuffer.Update(context, Matrix4x4.Transpose(Matrix4x4.CreateScale(camera.Transform.Far) * Matrix4x4.CreateTranslation(camera.Transform.Position)));
            sphere.Bind(context);
            pipeline.Begin(context);
            context.DrawIndexed((uint)sphere.IndexBuffer.Count, 0, 0);
            pipeline.End(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Destroy()
        {
            pipeline.Dispose();
            pipeline = null;
            constantBuffer.Dispose();
            sphere.Dispose();
            Texture.Dispose();
            Texture = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoundingBox GetBoundingBox()
        {
            return new BoundingBox(new Vector3(float.NaN), new Vector3(float.NaN));
        }
    }
}