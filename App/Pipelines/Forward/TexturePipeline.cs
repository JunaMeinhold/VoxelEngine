namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Buffers;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;

    public class TexturePipeline : IShaderLogic
    {
        private ConstantBuffer<ModelViewProjBuffer> mvpBuffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, out ShaderDescription description)
        {
            description = new();
            description.VertexShader = new("forward/texture/vs.hlsl", "main", VertexShaderVersion.VS_5_0);
            description.PixelShader = new("forward/texture/ps.hlsl", "main", PixelShaderVersion.PS_5_0);
            description.InputElements = ShaderDescription.GenerateInputElements<OrthoVertex>();
            mvpBuffer = new(device, ShaderStage.Vertex, 0);
            description.ConstantBuffers = new IConstantBuffer[] { mvpBuffer };
            description.Rasterizer = RasterizerDescription.CullBack;
            description.DepthStencil = DepthStencilDescription.None;
            description.Blend = BlendDescription.AlphaBlend;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, Matrix4x4 transform)
        {
            mvpBuffer.Write(context, new ModelViewProjBuffer(view, transform));
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}