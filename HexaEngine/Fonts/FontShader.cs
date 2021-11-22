using HexaEngine.Resources.Buffers;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Shaders;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Fonts
{
    public class FontShader : Shader
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct PixelBuffer
        {
            public Vector4 pixelColor;
        }

        // Properties
        public ID3D11Buffer ConstantMatrixBuffer { get; set; }

        public ID3D11Buffer ConstantPixelBuffer { get; set; }

        public IView View { get; set; }

        // Constructor
        public FontShader()
        {
            VertexShaderDescription = new("font/FontVertex.hlsl", "FontVertexShader", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("font/FontPixel.hlsl", "FontPixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));
            Initialize();

            // Setup the description of the dynamic matrix constant buffer that is in the vertex shader.
            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer2>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            ConstantMatrixBuffer = Manager.ID3D11Device.CreateBuffer(matrixBufferDesc);

            var pixelBufferDesc = new BufferDescription(Marshal.SizeOf<PixelBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            ConstantPixelBuffer = Manager.ID3D11Device.CreateBuffer(pixelBufferDesc);
        }

        public void Render(Matrix4x4 mvp, TextBase text)
        {
            text.Render(Manager.ID3D11DeviceContext);
            Write(ConstantMatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Transpose(mvp)
            });

            Write(ConstantPixelBuffer, new PixelBuffer()
            {
                pixelColor = text.Color
            });

            Manager.ID3D11DeviceContext.PSSetConstantBuffer(0, ConstantPixelBuffer);

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, ConstantMatrixBuffer);
            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);

            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.DrawIndexed(text.IndexCount, 0, 0);
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            ConstantMatrixBuffer.Dispose();
            ConstantPixelBuffer.Dispose();
            base.Dispose(disposing);
        }
    }
}