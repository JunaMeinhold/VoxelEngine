namespace HexaEngine.Shaders.BuildIn.Texture
{
    using HexaEngine.Resources.Buffers;
    using HexaEngine.Scenes.Interfaces;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;

    public class TextureShader : Shader
    {
        public ID3D11Buffer MatrixBuffer;

        public TextureShader()
        {
            VertexShaderDescription = new("texture/VertexShader.hlsl", "main", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("texture/PixelShader.hlsl", "main", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXTURE", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0));
            Initialize();

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer2>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            Write(MatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Transpose(transform * view.ViewMatrix * view.ProjectionMatrix),
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.DrawIndexed(indexCount, 0, 0);
        }

        public void Render(Matrix4x4 mvp, int indexCount)
        {
            Write(MatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Transpose(mvp),
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