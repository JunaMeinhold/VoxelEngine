namespace HexaEngine.Shaders.BuildIn.Color
{
    using HexaEngine.Resources.Buffers;
    using HexaEngine.Scenes.Interfaces;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;

    public class ColorShader : Shader
    {
        public ID3D11Buffer MatrixBuffer;

        protected override void Initialize()
        {
            VertexShaderDescription = new("color/VertexShader.hlsl", "main", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("color/PixelShader.hlsl", "main", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("COLOR", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer2>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            if (IsInvalid) return;
            Write(MatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Transpose(transform * view.ViewMatrix * view.ProjectionMatrix),
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.PSSetShaderResource(0, null);
            Manager.ID3D11DeviceContext.DrawIndexed(indexCount, 0, 0);
        }
    }
}