namespace App.Pipelines.Effects
{
    using System.Numerics;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Lightning;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Scenes;

    public enum EffectFlags
    {
        None = 0,
        NoInput = 1,
        NoOutput = 2,
        PrePass = 4,
    }

    public interface IEffect
    {
        bool Enabled { get; set; }

        EffectFlags Flags { get; }

        public void Update(ID3D11DeviceContext context);

        public void PrePass(ID3D11DeviceContext context)
        {
        }

        public void Pass(ID3D11DeviceContext context);
    }

    public class GodRaysEffect
    {
        private Viewport viewport;
        private VoxelEngine.Graphics.Primitives.Plane plane;
        private ConstantBuffer<SunParams> paramsSunBuffer;
        private ConstantBuffer<CBWorld> paramsWorldBuffer;
        private ConstantBuffer<CBCamera> cameraBuffer;
        private ID3D11SamplerState sunSampler;
        private GraphicsPipeline sun;

        private ConstantBuffer<GodRaysParams> paramsBuffer;
        private ID3D11SamplerState sampler;
        private GraphicsPipeline godrays;

        private Texture2D sunsprite;
        private Texture2D sunBuffer;
        private Texture2D noiseTex;

        private float godraysDensity = 0.975f;
        private float godraysWeight = 0.25f;
        private float godraysDecay = 0.825f;
        private float godraysExposure = 2.0f;
        private readonly ID3D11Device device;

        public struct GodRaysParams
        {
            public Vector4 ScreenSpacePosition;
            public float GodraysDensity;
            public float GodraysWeight;
            public float GodraysDecay;
            public float GodraysExposure;
            public Vector4 Color;
        }

        public struct SunParams
        {
            public Vector3 Diffuse;
            public float AlbedoFactor;
        }

        public GodRaysEffect(ID3D11Device device, int width, int height)
        {
            plane = new(device, 5);

            sun = new(device, new()
            {
                VertexShader = "sun/vs.hlsl",
                PixelShader = "sun/ps.hlsl"
            }, new GraphicsPipelineState()
            {
                Blend = BlendDescription.AlphaBlend,
                BlendFactor = Vector4.One,
                DepthStencil = DepthStencilDescription.DepthRead,
                Rasterizer = RasterizerDescription.CullBack,
                SampleMask = int.MaxValue,
                StencilRef = 0,
                Topology = PrimitiveTopology.TriangleList
            });
            sunSampler = device.CreateSamplerState(SamplerDescription.LinearWrap);

            paramsSunBuffer = new(device, CpuAccessFlags.Write);
            paramsWorldBuffer = new(device, CpuAccessFlags.Write);
            cameraBuffer = new(device, CpuAccessFlags.Write);

            godrays = new(device, new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "godrays/ps.hlsl"
            },
            new GraphicsPipelineState()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Blend = BlendDescription.Additive,
                Topology = PrimitiveTopology.TriangleStrip,
                BlendFactor = default,
                SampleMask = int.MaxValue
            });
            sampler = device.CreateSamplerState(SamplerDescription.LinearClamp);

            paramsBuffer = new(device, CpuAccessFlags.Write);

            sunsprite = new(device, "sun/sunsprite.png");
            sunBuffer = new(device, Format.R16G16B16A16_Float, width, height, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);

            noiseTex = new(device, Format.R32_Float, 1024, 1024, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);
            this.device = device;
            viewport = new(width, height);
        }

        public void Resize(int width, int height)
        {
            sunBuffer.Resize(device, Format.R16G16B16A16_Float, width, height, 1, 1, CpuAccessFlags.None);
            viewport = new(width, height);
        }

        public void Update(ID3D11DeviceContext context, Camera camera, DirectionalLight light)
        {
            GodRaysParams raysParams = default;

            raysParams.GodraysDecay = godraysDecay;
            raysParams.GodraysWeight = godraysWeight;
            raysParams.GodraysDensity = godraysDensity;
            raysParams.GodraysExposure = godraysExposure;
            raysParams.Color = light.Color;

            var camera_position = camera.Transform.GlobalPosition;

            var translation = Matrix4x4.CreateTranslation(camera_position);

            var far = camera.Transform.Far;
            var light_position = Vector3.Transform(light.Transform.Backward * (far / 2f), translation);

            var transform = Matrix4x4.CreateTranslation(light.Transform.Backward * (far / 15));

            var light_posH = Vector4.Transform(light_position, camera.Transform.ViewProjection);
            var ss_sun_pos = new Vector4(0.5f * light_posH.X / light_posH.W + 0.5f, -0.5f * light_posH.Y / light_posH.W + 0.5f, light_posH.Z / light_posH.W, 1.0f);

            raysParams.ScreenSpacePosition = ss_sun_pos;

            paramsBuffer.Update(context, raysParams);

            CBWorld world = default;

            world.World = Matrix4x4.Transpose(transform);
            world.WorldInv = Matrix4x4.Transpose(light.Transform.GlobalInverse);

            paramsWorldBuffer.Update(context, world);

            SunParams sunParams = default;

            sunParams.Diffuse = Vector3.One;
            sunParams.AlbedoFactor = 1f;

            paramsSunBuffer.Update(context, sunParams);
            cameraBuffer.Update(context, new CBCamera(camera, viewport));
        }

        public void PrePass(ID3D11DeviceContext context, DepthStencil depth)
        {
            context.ClearRenderTargetView(sunBuffer.RTV, default);
            context.OMSetRenderTargets(sunBuffer.RTV, depth.DSV);
            context.RSSetViewport(viewport);
            context.VSSetConstantBuffer(0, paramsWorldBuffer);
            context.VSSetConstantBuffer(1, cameraBuffer);
            context.PSSetConstantBuffer(0, paramsSunBuffer);
            context.PSSetShaderResource(0, sunsprite.SRV);
            context.PSSetSampler(0, sunSampler);
            plane.DrawAuto(context, sun);
            context.ClearState();
        }

        public void Pass(ID3D11DeviceContext context)
        {
            context.PSSetConstantBuffer(0, paramsBuffer);
            context.PSSetShaderResource(0, sunBuffer.SRV);
            context.PSSetSampler(0, sampler);
            godrays.Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            context.ClearState();
        }

        public void Dispose()
        {
            plane.Dispose();
            sun.Dispose();
            sunSampler.Dispose();
            paramsSunBuffer.Dispose();
            paramsWorldBuffer.Dispose();
            cameraBuffer.Dispose();

            godrays.Dispose();
            sampler.Dispose();
            paramsBuffer.Dispose();

            sunsprite.Dispose();
            sunBuffer.Dispose();
            noiseTex.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}