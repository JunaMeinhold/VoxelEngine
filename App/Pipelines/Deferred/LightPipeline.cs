namespace App.Pipelines.Deferred
{
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Lightning;
    using VoxelEngine.Rendering.Shaders;
    using VoxelEngine.Scenes;

    public class LightPipeline : GraphicsPipeline
    {
        private readonly ConstantBuffer<CBCamera> cameraBuffer;
        private readonly ConstantBuffer<CBDirectionalLightSD> directionalLightBuffer;

        public LightPipeline(ID3D11Device device) : base(device, new()
        {
            VertexShader = "quad.hlsl",
            PixelShader = "deferred/light/ps.hlsl",
        }, new GraphicsPipelineState()
        {
            Blend = BlendDescription.Additive,
            Topology = PrimitiveTopology.TriangleStrip
        })
        {
            cameraBuffer = new(device, CpuAccessFlags.Write);
            directionalLightBuffer = new(device, CpuAccessFlags.Write);
            ConstantBuffers.Append(directionalLightBuffer, ShaderStage.Pixel);
            ConstantBuffers.Append(cameraBuffer, ShaderStage.Pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, CBCamera camera, CBDirectionalLightSD light)
        {
            cameraBuffer.Update(context, camera);
            directionalLightBuffer.Update(context, light);
        }

        public void Pass(ID3D11DeviceContext context)
        {
            Begin(context);
            context.DrawInstanced(4, 1, 0, 0);
            End(context);
        }
    }
}