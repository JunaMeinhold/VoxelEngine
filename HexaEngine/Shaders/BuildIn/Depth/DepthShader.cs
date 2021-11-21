using HexaEngine.Resources;
using HexaEngine.Resources.Buffers;
using HexaEngine.Scenes.Interfaces;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Shaders.BuildIn
{
    public class DepthShader : Shader
    {
        public ID3D11Buffer MatrixBuffer;
        public RenderTexture DepthMap;

        protected override void Initialize()
        {
            DepthMap = new RenderTexture();
            _ = DepthMap.Initialize(Manager.ID3D11Device, nameof(DepthShader), 1024, 1024);

            VertexShaderDescription = new("depth/VertexShader.hlsl", "main", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("depth/PixelShader.hlsl", "main", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            if (IsInvalid) return;
            Write(MatrixBuffer, new PerFrameBuffer()
            {
                Projection = Matrix4x4.Transpose(view.ProjectionMatrix),
                View = Matrix4x4.Transpose(view.ViewMatrix),
                World = Matrix4x4.Transpose(transform)
            });

            //Manager.SetState(Manager.FrontCullingRasterizerState);
            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.PSSetShaderResource(0, null);
            Manager.ID3D11DeviceContext.DrawIndexed(indexCount, 0, 0);
            //Manager.RestoreState();
        }

        protected override void Dispose(bool disposing)
        {
            MatrixBuffer.Dispose();
            MatrixBuffer = null;
            DepthMap.Dispose();
            DepthMap = null;
            base.Dispose(disposing);
        }
    }
}