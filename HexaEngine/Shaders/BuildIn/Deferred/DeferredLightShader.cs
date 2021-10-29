using HexaEngine.Extensions;
using HexaEngine.Resources;
using HexaEngine.Resources.Buffers;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Windows;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Shaders.BuildIn.Deferred
{
    public class DeferredLightShader : Shader
    {
        public Matrix4x4 viewMatrix = Matrix4x4.Transpose(MatrixExtensions.LookAtLH(-Vector3.UnitZ, Vector3.UnitZ + -Vector3.UnitZ, Vector3.UnitY));
        public Matrix4x4 projectMatrix;
        private DirectionalLight directional;
        public readonly ID3D11Buffer MatrixBuffer;
        public readonly ID3D11Buffer LightBuffer;
        public readonly ID3D11Buffer FogBuffer;
        public readonly GBuffers GBuffers;
        public readonly RenderPlane RenderPlane;

        public DirectionalLight Directional { get => directional; set { directional = value; } }

        public DepthShader DepthShader { get; set; }

        public FogDescription FogDescription { get; set; }

        [StructLayout(LayoutKind.Sequential)]
        public struct BufferLightType
        {
            public Vector3 LightDirection;
            public FogDescription FogDescription;
            public float padd;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColorDescription
        {
            public float Gamma;
            public float Vibrance;
            public Vector3 Vibrance_RGB_balance;
        }

        public DeferredLightShader()
        {
            DepthShader = new();
            GBuffers = new();
            GBuffers.Initialize(Manager.ID3D11Device, Manager.Width, Manager.Height);
            RenderPlane = new(Manager, nameof(DeferredLightShader));
            VertexShaderDescription = new("deferred/LightVertexShader.hlsl", "LightVertexShader", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("deferred/LightPixelShader.hlsl", "LightPixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));
            Initialize();

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer2>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));

            var lightBufferDesc = new BufferDescription(Marshal.SizeOf<BufferLightType>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            LightBuffer = CreateBuffer(lightBufferDesc, nameof(LightBuffer));

            var fogBufferDesc = new BufferDescription(Marshal.SizeOf<FogDescription>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            FogBuffer = CreateBuffer(fogBufferDesc, nameof(FogBuffer));

            Manager.OnResize += Manager_OnResize;
            Manager_OnResize(null, null);
        }

        private void Manager_OnResize(object sender, System.EventArgs e)
        {
            projectMatrix = Matrix4x4.Transpose(MatrixExtensions.OrthoLH(Manager.Width, Manager.Height, 0.01f, 10000f));
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            Manager.ID3D11DeviceContext.RSSetState(Manager.DefaultRasterizerState);

            Manager.SwitchDepth(false);

            Write(MatrixBuffer, new PerFrameBuffer()
            {
                Projection = projectMatrix,
                View = viewMatrix,
                World = Matrix4x4.Identity
            });

            Write(LightBuffer, new BufferLightType()
            {
                LightDirection = Directional.Direction
            });

            Write(FogBuffer, FogDescription);

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.PSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.PSSetConstantBuffer(1, LightBuffer);
            Manager.ID3D11DeviceContext.PSSetConstantBuffer(2, FogBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            RenderPlane.Render(Manager.ID3D11DeviceContext);
            GBuffers.Render(Manager.ID3D11DeviceContext);
            Manager.ID3D11DeviceContext.DrawIndexed(RenderPlane.IndexCount, 0, 0);
            Manager.SwitchDepth(true);
            Manager.ID3D11DeviceContext.RSSetState(Manager.CurrentRasterizerState);
            Manager.ID3D11DeviceContext.PSSetShaderResources(0, new ID3D11ShaderResourceView[5]);

            DepthShader.Render(view, transform, indexCount);
        }

        public void Render()
        {
            Manager.ID3D11DeviceContext.RSSetState(Manager.DefaultRasterizerState);
            Manager.SwitchDepth(false);

            Write(MatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Identity * viewMatrix * projectMatrix
            });

            Write(LightBuffer, new BufferLightType()
            {
                LightDirection = Directional.Direction,
                FogDescription = FogDescription,
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.PSSetConstantBuffer(0, LightBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            RenderPlane.Render(Manager.ID3D11DeviceContext);
            GBuffers.Render(Manager.ID3D11DeviceContext);
            Manager.ID3D11DeviceContext.DrawIndexed(RenderPlane.IndexCount, 0, 0);
            Manager.SwitchDepth(true);
            Manager.ID3D11DeviceContext.RSSetState(Manager.CurrentRasterizerState);
            Manager.ID3D11DeviceContext.PSSetShaderResources(0, new ID3D11ShaderResourceView[5]);
        }

        protected override void Dispose(bool disposing)
        {
            MatrixBuffer.Dispose();
            LightBuffer.Dispose();
            FogBuffer.Dispose();
            GBuffers.Dispose();
            DepthShader.Dispose();
            RenderPlane.Dispose();
            base.Dispose(disposing);
        }
    }
}