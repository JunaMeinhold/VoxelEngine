using HexaEngine.Resources;
using HexaEngine.Resources.Buffers;
using HexaEngine.Scenes.Interfaces;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Shaders.BuildIn.Deferred
{
    public class DeferredLightShader : Shader
    {
        public Matrix4x4 viewMatrix = Matrix4x4.Transpose(Mathematics.Extensions.LookAtLH(-Vector3.UnitZ, Vector3.UnitZ + -Vector3.UnitZ, Vector3.UnitY));
        public Matrix4x4 projectMatrix;
        private DirectionalLight directional;
        private ID3D11Buffer MatrixBuffer;
        private ID3D11Buffer LightBuffer;
        private GBuffers _gBuffers;
        private RenderPlane RenderPlane;

        public DirectionalLight Directional
        { get => directional; set { directional = value; } }

        public DepthShader DepthShader { get; set; }

        public FogDescription FogDescription { get; set; }

        public GBuffers GBuffers => _gBuffers;

        [StructLayout(LayoutKind.Sequential)]
        public struct DirectionalLightDescription
        {
            public Vector4 Color;
            public Vector3 LightDirection;
            public float reserved;
            public Matrix4x4 View;
            public Matrix4x4 Projection;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LightDescription
        {
            public Vector3 Position;
            public Vector4 Color;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CamBuffer
        {
            public Vector3 Position;
            public Matrix4x4 CameraViewToWorldMatrix;
            public float reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BufferLightType
        {
            public DirectionalLightDescription LightDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColorDescription
        {
            public float Gamma;
            public float Vibrance;
            public Vector3 Vibrance_RGB_balance;
        }

        protected override void Initialize()
        {
            DepthShader = new();
            _gBuffers = new();
            _ = _gBuffers.Initialize(Manager.ID3D11Device, Manager.Width, Manager.Height);
            RenderPlane = new(Manager, nameof(DeferredLightShader));
            VertexShaderDescription = new("deferred/LightVertexShader.hlsl", "LightVertexShader", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("deferred/LightPixelShader.hlsl", "LightPixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0, InputClassification.PerVertexData, 0));
            InputElements.Add(new("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0, InputClassification.PerVertexData, 0));

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));

            var lightBufferDesc = new BufferDescription(Marshal.SizeOf<BufferLightType>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            LightBuffer = CreateBuffer(lightBufferDesc, nameof(LightBuffer));

            Manager.OnResize += Manager_OnResize;
            Manager_OnResize(null, null);
        }

        private void Manager_OnResize(object sender, System.EventArgs e)
        {
            projectMatrix = Matrix4x4.Transpose(Mathematics.Extensions.OrthoLH(Manager.Width, Manager.Height, 0.01f, 10000f));
        }

        public override unsafe void Render(IView view, Matrix4x4 transform, int indexCount)
        {
        }

        public void Render(IView view)
        {
            if (IsInvalid) return;
            Manager.ID3D11DeviceContext.RSSetState(Manager.DefaultRasterizerState);
            Manager.SwitchDepth(false);

            Write(MatrixBuffer, new PerFrameBuffer()
            {
                World = Matrix4x4.Identity,
                View = viewMatrix,
                Projection = projectMatrix
            });

            Write(LightBuffer, new BufferLightType()
            {
                LightDescription = new()
                {
                    Color = Directional.DiffuseColor,
                    LightDirection = Directional.Direction,
                    View = Matrix4x4.Transpose(Directional.ViewMatrix),
                    Projection = Matrix4x4.Transpose(Directional.ProjectionMatrix),
                },
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.PSSetConstantBuffer(0, LightBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            RenderPlane.Render(Manager.ID3D11DeviceContext);
            _gBuffers.Render(Manager.ID3D11DeviceContext);
            Manager.ID3D11DeviceContext.PSSetShaderResource(3, Directional.ShadowMap.ShaderResourceView);
            Manager.ID3D11DeviceContext.DrawIndexed(RenderPlane.IndexCount, 0, 0);
            Manager.SwitchDepth(true);
            Manager.ID3D11DeviceContext.RSSetState(Manager.CurrentRasterizerState);
            Manager.ID3D11DeviceContext.PSSetShaderResources(0, new ID3D11ShaderResourceView[6]);
        }

        protected override void Dispose(bool disposing)
        {
            MatrixBuffer.Dispose();
            LightBuffer.Dispose();
            _gBuffers.Dispose();
            DepthShader.Dispose();
            RenderPlane.Dispose();
            base.Dispose(disposing);
        }
    }
}