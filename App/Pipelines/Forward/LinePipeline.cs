namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Buffers;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;

    public class LinePipeline : IShaderLogic
    {
        private ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private ConstantBuffer<Color4> colorBuffer;

        public Color4 Color;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, Matrix4x4 transform)
        {
            mvpBuffer.Write(context, new ModelViewProjBuffer(view, transform));
            colorBuffer.Write(context, Color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, out ShaderDescription description)
        {
            description = new();
            description.VertexShader = new("forward/line/vertex.hlsl", "main", VertexShaderVersion.VS_5_0);
            description.PixelShader = new("forward/line/pixel.hlsl", "main", PixelShaderVersion.PS_5_0);
            description.InputElements = ShaderDescription.GenerateInputElements<LineVertex>();
            mvpBuffer = new(device, ShaderStage.Vertex, 0);
            colorBuffer = new(device, ShaderStage.Pixel, 0);
            description.ConstantBuffers = new IConstantBuffer[] { mvpBuffer, colorBuffer };
            description.Rasterizer = RasterizerDescription.CullBack;
            description.DepthStencil = DepthStencilDescription.Default;
            description.Blend = BlendDescription.Opaque;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}