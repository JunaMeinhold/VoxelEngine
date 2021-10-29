using HexaEngine.Scenes;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace HexaEngine.Particles
{
    public class ParticleShader : Shader
    {        // Structs
        [StructLayout(LayoutKind.Sequential)]
        internal struct MatrixBufferType
        {
            public Matrix4x4 World;
            public Matrix4x4 View;
            public Matrix4x4 Projection;
        }

        public ID3D11Buffer ConstantMatrixBuffer { get; set; }

        public ID3D11SamplerState SamplerState { get; set; }

        // Constructor
        public ParticleShader()
        {
            VertexShaderDescription = new("particle/ParticleVertexShader.hlsl", "ParticleVertexShader", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("particle/ParticlePixelShader.hlsl", "ParticlePixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("COLOR", 0, Format.R32G32B32A32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));
            Initialize();

            ConstantMatrixBuffer = CreateBuffer(new()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = Marshal.SizeOf<MatrixBufferType>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            }, nameof(ConstantMatrixBuffer));

            SamplerState = CreateSamplerState(new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0,
                MaxAnisotropy = 1,
                ComparisonFunction = ComparisonFunction.Always,
                BorderColor = new Color4(0, 0, 0, 0),  // Black Border.
                MinLOD = 0,
                MaxLOD = float.MaxValue
            }, nameof(SamplerState));
        }

        protected override void Dispose(bool disposing)
        {
            ConstantMatrixBuffer.Dispose();
            ConstantMatrixBuffer = null;
            SamplerState.Dispose();
            SamplerState = null;
            base.Dispose(disposing);
        }

        public virtual void SetParameters(IView view, ParticleSystem system)
        {
            // Copy the passed in matrices into the constant buffer.
            Write(ConstantMatrixBuffer, new MatrixBufferType()
            {
                World = Matrix4x4.Transpose(system.GlobalPose),
                View = Matrix4x4.Transpose(view.ViewMatrix),
                Projection = Matrix4x4.Transpose(view.ProjectionMatrix)
            });

            // Finally set the constant buffer in the vertex shader with the updated values.
            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, ConstantMatrixBuffer);

            // Set shader resource in the pixel shader.
            Manager.ID3D11DeviceContext.PSSetShaderResource(0, system.Texture);
        }

        public virtual void Render(IView view, ParticleSystem system)
        {
            SetParameters(view, system);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);
            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.PSSetSampler(0, SamplerState);

            Manager.ID3D11DeviceContext.IASetVertexBuffers(0, new VertexBufferView(system.VertexBuffer, Marshal.SizeOf<ParticleSystem.VertexType>(), 0));
            Manager.ID3D11DeviceContext.IASetIndexBuffer(system.IndexBuffer, Format.R32_UInt, 0);

            Manager.ID3D11DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            Manager.ID3D11DeviceContext.DrawIndexed(system.IndexCount, 0, 0);
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            throw new NotImplementedException();
        }
    }
}