namespace App.Pipelines.Forward
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Scenes;

    public class LightPipeline : Pipeline
    {
        private readonly ID3D11Buffer cameraBuffer;
        private readonly ID3D11Buffer directionalLightBuffer;

        public LightPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "deferred/light/vs.hlsl",
            PixelShader = "deferred/light/ps.hlsl",
            Rasterizer = RasterizerDescription.CullBack,
            DepthStencil = DepthStencilDescription.Default,
            Blend = BlendDescription.Additive,
            Topology = PrimitiveTopology.TriangleList,
        })
        {
            cameraBuffer = device.CreateBuffer(Marshal.SizeOf<CBCamera>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
            directionalLightBuffer = device.CreateBuffer(Marshal.SizeOf<CBDirectionalLightSD>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
            ConstantBuffers.Append(directionalLightBuffer, ShaderStage.Pixel);
            ConstantBuffers.Append(cameraBuffer, ShaderStage.Pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, CBCamera camera, CBDirectionalLightSD light)
        {
            DeviceHelper.Write(context, cameraBuffer, camera);
            DeviceHelper.Write(context, directionalLightBuffer, light);
        }
    }
}