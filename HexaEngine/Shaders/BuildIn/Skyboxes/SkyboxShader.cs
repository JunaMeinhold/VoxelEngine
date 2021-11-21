namespace HexaEngine.Shaders.BuildIn.Skyboxes
{
    using HexaEngine.Resources.Buffers;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Scenes.Objects;
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;

    public class SkyboxShader : Shader
    {
        private ID3D11Buffer MatrixBuffer;

        protected override void Initialize()
        {
            VertexShaderDescription = new("skybox/VertexShader.hlsl", "SkyboxVertexShader", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("skybox/PixelShader.hlsl", "SkyboxPixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));
        }

        /// <summary>
        /// Warning NotImplemented
        /// </summary>
        public override void Render(IView view, Matrix4x4 transform = default, int indexCount = 0)
        {
            throw new NotImplementedException();
        }

        public void Render(IView view, Skybox skybox)
        {
            if (IsInvalid) return;
            if (skybox is null) return;
            Manager.SetRenderTarget();
            Write(MatrixBuffer, new PerFrameBuffer()
            {
                Projection = Matrix4x4.Transpose(view.ProjectionMatrix), // ProjectionMatrix from main camera (perspective)
                View = Matrix4x4.Transpose(view.ViewMatrix), // View of the main camera
                World = Matrix4x4.Transpose(Matrix4x4.CreateScale(view.FarPlane) * Matrix4x4.CreateTranslation(view.Position)) // position of the main camera
            });

            Manager.SetStencil(Manager.DepthStencilStateSkybox);
            Manager.SetState(Manager.NoCullingRasterizerState);
            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);
            skybox.Render(Manager.ID3D11DeviceContext);
            Manager.ID3D11DeviceContext.PSSetSampler(0, skybox.Texture.SamplerState);
            Manager.ID3D11DeviceContext.DrawIndexed(skybox.Model.Indices.Length, 0, 0);
            Manager.RestoreState();
            Manager.RestoreStencil();
        }

        protected override void Dispose(bool disposing)
        {
            MatrixBuffer.Dispose();
            base.Dispose(disposing);
        }
    }
}