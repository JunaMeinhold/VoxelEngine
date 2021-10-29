using HexaEngine.Scenes;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Fonts
{
    public class FontShader : Shader
    {
        // Structs
        [StructLayout(LayoutKind.Sequential)]
        internal struct MatrixBuffer
        {
            public Matrix4x4 World;
            public Matrix4x4 View;
            public Matrix4x4 Projection;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PixelBuffer
        {
            public Vector4 pixelColor;
        }

        // Properties
        public ID3D11Buffer ConstantMatrixBuffer { get; set; }

        public ID3D11SamplerState SamplerState { get; set; }
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
            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<MatrixBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            ConstantMatrixBuffer = Manager.ID3D11Device.CreateBuffer(matrixBufferDesc);

            // Create a texture sampler state description.
            var samplerDesc = new SamplerDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0,
                MaxAnisotropy = 1,
                ComparisonFunction = ComparisonFunction.Always,
                BorderColor = new Vortice.Mathematics.Color4(0, 0, 0, 0),
                MinLOD = 0,
                MaxLOD = float.MaxValue
            };

            // Create the texture sampler state.
            SamplerState = Manager.ID3D11Device.CreateSamplerState(samplerDesc);

            var pixelBufferDesc = new BufferDescription(Marshal.SizeOf<PixelBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            ConstantPixelBuffer = Manager.ID3D11Device.CreateBuffer(pixelBufferDesc);
        }

        /*
        public void Dispose()
        {
            // Release the pixel constant buffer.
            ConstantPixelBuffer?.Dispose();
            ConstantPixelBuffer = null;
            // Release the sampler state.
            SamplerState?.Dispose();
            SamplerState = null;
            // Release the matrix constant buffer.
            ConstantMatrixBuffer?.Dispose();
            ConstantMatrixBuffer = null;
            // Release the layout.
            Layout?.Dispose();
            Layout = null;
            // Release the pixel shader.
            PixelShader?.Dispose();
            PixelShader = null;
            // Release the vertex shader.
            VertexShader?.Dispose();
            VertexShader = null;
        }*/

        public bool Render(ID3D11DeviceContext deviceContext, int indexCount, Matrix4x4 worldMatrix, ID3D11ShaderResourceView texture, Vector4 pixelColor)
        {
            // Set the shader parameters that it will use for rendering.
            SetShaderParameters(deviceContext, worldMatrix, texture, pixelColor);

            // Now render the prepared buffers with the shader.
            RenderShader(deviceContext, indexCount);

            return true;
        }

        private void SetShaderParameters(ID3D11DeviceContext deviceContext, Matrix4x4 worldMatrix, ID3D11ShaderResourceView texture, Vector4 pixelColor)
        {
            Write(ConstantMatrixBuffer, new MatrixBuffer()
            {
                Projection = Matrix4x4.Transpose(View.ProjectionMatrix),
                View = Matrix4x4.Transpose(View.ViewMatrix),
                World = Matrix4x4.Transpose(worldMatrix)
            });

            deviceContext.VSSetConstantBuffer(0, ConstantMatrixBuffer);
            deviceContext.PSSetShaderResource(0, texture);

            Write(ConstantPixelBuffer, new PixelBuffer()
            {
                pixelColor = pixelColor
            });

            deviceContext.PSSetConstantBuffer(0, ConstantPixelBuffer);
        }

        private void RenderShader(ID3D11DeviceContext deviceContext, int indexCount)
        {
            deviceContext.IASetInputLayout(InputLayout);

            deviceContext.VSSetShader(VertexShader);
            deviceContext.PSSetShader(PixelShader);

            deviceContext.PSSetSampler(0, SamplerState);

            deviceContext.DrawIndexed(indexCount, 0, 0);
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            throw new NotImplementedException();
        }
    }
}