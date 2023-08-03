namespace App.Pipelines.Forward
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Core;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Rendering.D3D;
    using VoxelEngine.Rendering.D3D.Buffers;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;

    public class SkyboxPipeline : IShaderLogic
    {
        private ConstantBuffer<ModelViewProjBuffer> mvpBuffer;
        private ConstantBuffer<Vector4> blendBuffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ID3D11DeviceContext context, IView view, Matrix4x4 transform)
        {
            var blendV = new Vector4(MathF.Max(3 * MathF.Sin(Time.GameTimeNormalized * MathF.PI) - 2, 0));
            blendBuffer.Write(context, blendV);
            mvpBuffer.Write(context, new ModelViewProjBuffer(view, Matrix4x4.CreateScale(view.Transform.Far) * Matrix4x4.CreateTranslation(view.Transform.Position)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(ID3D11Device device, out ShaderDescription description)
        {
            description = new();
            description.VertexShader = new("forward/skybox/vs.hlsl", "main", VertexShaderVersion.VS_5_0);
            description.PixelShader = new("forward/skybox/ps.hlsl", "main", PixelShaderVersion.PS_5_0);
            description.InputElements = ShaderDescription.GenerateInputElements<Vertex>();
            mvpBuffer = new(device, ShaderStage.Vertex, 0);
            blendBuffer = new(device, ShaderStage.Pixel, 0);
            description.ConstantBuffers = new IConstantBuffer[] { mvpBuffer, blendBuffer };
            description.Rasterizer = RasterizerDescription.CullNone;
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