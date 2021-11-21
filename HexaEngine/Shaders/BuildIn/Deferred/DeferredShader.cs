using HexaEngine.Resources.Buffers;
using HexaEngine.Scenes.Interfaces;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Shaders.BuildIn.Deferred
{
    public class DeferredShader : Shader
    {
        private ID3D11Buffer MatrixBuffer;

        protected override void Initialize()
        {
            VertexShaderDescription = new("deferred/VertexShader.hlsl", "main", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("deferred/PixelShader.hlsl", "GPixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXCOORD", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = Manager.ID3D11Device.CreateBuffer(matrixBufferDesc);
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            if (IsInvalid) return;
            Write(MatrixBuffer, new PerFrameBuffer()
            {
                Projection = Matrix4x4.Transpose(view.ProjectionMatrix),
                View = Matrix4x4.Transpose(view.ViewMatrix),
                World = Matrix4x4.Transpose(transform),
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.DrawIndexed(indexCount, 0, 0);
        }

        protected override void Dispose(bool disposing)
        {
            MatrixBuffer.Dispose();
            base.Dispose(disposing);
        }
    }
}