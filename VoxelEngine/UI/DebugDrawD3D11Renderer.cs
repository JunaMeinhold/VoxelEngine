using HexaEngine.Editor;

namespace HexaEngine.Rendering.Renderers
{
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using HexaEngine.ImGuiNET;
    using Newtonsoft.Json.Linq;
    using Vortice.D3DCompiler;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Mathematics;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using MapFlags = Vortice.Direct3D11.MapFlags;

    public unsafe class DebugDrawD3D11Renderer : IDisposable
    {
        private static ID3D11Device device;
        private static ID3D11DeviceContext context;
        private static ID3D11VertexShader vertexShader;
        private static ID3D11PixelShader pixelShader;
        private static ID3D11DepthStencilState depthStencilState;
        private static ID3D11BlendState blendState;
        private static ID3D11RasterizerState rasterizerState;
        private static ID3D11InputLayout inputLayout;
        private static Blob vertexShaderBlob;
        private static Blob pixelShaderBlob;
        private static ID3D11Buffer vertexBuffer;
        private static ID3D11Buffer indexBuffer;
        private static ID3D11Buffer constantBuffer;

        private int vertexBufferSize = 5000;
        private int indexBufferSize = 10000;
        private bool disposedValue;

        public DebugDrawD3D11Renderer(ID3D11Device device, ID3D11DeviceContext context)
        {
            DebugDrawD3D11Renderer.device = device;
            DebugDrawD3D11Renderer.context = context;

            string vertexShaderCode =
                 @"
struct VS_INPUT
{
	float3 position : POSITION;
    float2 tex : TEXCOORD0;
	float4 color : COLOR0;
};
struct PS_INPUT
{
	float4 position : SV_POSITION;
	float4 color : COLOR0;
    float2 tex : TEXCOORD0;
};
cbuffer MVPBuffer
{
    float4x4 ProjectionMatrix;
};
PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    output.position = mul(float4(input.position, 1), ProjectionMatrix);
    output.tex = input.tex;
	output.color = input.color;
	return output;
}";

            Compiler.Compile(vertexShaderCode, "main", "vs", "vs_4_0", out vertexShaderBlob, out Blob errorBlob);
            if (vertexShaderBlob == null)
            {
                throw new Exception("error compiling vertex shader");
            }

            vertexShader = device.CreateVertexShader(vertexShaderBlob.AsBytes());

            InputElementDescription[] inputElements = new[]
            {
                new InputElementDescription( "POSITION", 0, Format.R32G32B32_Float,0,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   12, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 20, 0, InputClassification.PerVertexData, 0 ),
            };

            inputLayout = device.CreateInputLayout(inputElements, vertexShaderBlob);

            BufferDescription constBufferDesc = new()
            {
                ByteWidth = sizeof(Matrix4x4),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
            };
            constantBuffer = device.CreateBuffer(constBufferDesc);

            string pixelShaderCode =
                @"struct PS_INPUT
{
    float4 position : SV_POSITION;
	float4 color : COLOR0;
    float2 tex : TEXCOORD0;
};

float4 main(PS_INPUT pixel) : SV_TARGET
{
	return pixel.color;
}";

            Compiler.Compile(pixelShaderCode, "main", "ps", "ps_4_0", out pixelShaderBlob, out errorBlob);
            if (pixelShaderBlob == null)
            {
                throw new Exception("error compiling pixel shader");
            }

            pixelShader = device.CreatePixelShader(pixelShaderBlob.AsBytes());

            BlendDescription blendDesc = new()
            {
                AlphaToCoverageEnable = false
            };

            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                BlendOperationAlpha = BlendOperation.Add,
                IsBlendEnabled = true,
                BlendOperation = BlendOperation.Add,
                DestinationBlendAlpha = Blend.InverseSourceAlpha,
                DestinationBlend = Blend.InverseSourceAlpha,
                SourceBlend = Blend.SourceAlpha,
                SourceBlendAlpha = Blend.SourceAlpha,
                RenderTargetWriteMask = ColorWriteEnable.All
            };

            blendState = device.CreateBlendState(blendDesc);

            RasterizerDescription rasterDesc = new()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                ScissorEnable = false,
                DepthClipEnable = true,
                AntialiasedLineEnable = true,
                MultisampleEnable = false
            };

            rasterizerState = device.CreateRasterizerState(rasterDesc);

            DepthStencilOperationDescription stencilOpDesc = new(StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, ComparisonFunction.Always);
            DepthStencilDescription depthDesc = new()
            {
                DepthEnable = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.LessEqual,
                StencilEnable = false,
                FrontFace = stencilOpDesc,
                BackFace = stencilOpDesc
            };

            depthStencilState = device.CreateDepthStencilState(depthDesc);
        }

        public void BeginDraw()
        {
            DebugDraw.NewFrame();
        }

        public void EndDraw(ID3D11RenderTargetView rtv, ID3D11DepthStencilView dsv = null)
        {
            DebugDraw.Render();
            Render(DebugDraw.GetQueue(), DebugDraw.GetCamera(), rtv, dsv);
        }

        private static unsafe void SetupRenderState(Viewport drawData, ID3D11DeviceContext ctx)
        {
            var viewport = drawData;

            uint stride = (uint)sizeof(DebugDrawVert);
            uint offset = 0;

            ctx.VSSetShader(vertexShader);
            ctx.PSSetShader(pixelShader);
            ctx.IASetInputLayout(inputLayout);
            ctx.RSSetState(rasterizerState);
            ctx.OMSetDepthStencilState(depthStencilState);
            ctx.OMSetBlendState(blendState);
            ctx.RSSetViewport(viewport);
            ctx.IASetVertexBuffer(0, vertexBuffer, (int)stride, (int)offset);
            ctx.IASetIndexBuffer(indexBuffer, sizeof(ushort) == 2 ? Format.R16_UInt : Format.R32_UInt, 0);
            ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            ctx.VSSetConstantBuffer(0, constantBuffer);
        }

        private void Render(DebugDrawCommandQueue queue, Matrix4x4 camera, ID3D11RenderTargetView rtv, ID3D11DepthStencilView dsv)
        {
            if (queue.VertexCount > vertexBufferSize || vertexBuffer == null)
            {
                vertexBuffer?.Dispose();
                var newVertexBufferSize = (int)(queue.VertexCount * 1.5f);
                vertexBufferSize = newVertexBufferSize == 0 ? vertexBufferSize : newVertexBufferSize;
                vertexBuffer = device.CreateBuffer(new BufferDescription(vertexBufferSize * sizeof(DebugDrawVert), BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            }

            if (queue.IndexCount > indexBufferSize || indexBuffer == null)
            {
                indexBuffer?.Dispose();
                var newIndexBufferSize = (int)(queue.IndexCount * 1.5f);
                indexBufferSize = newIndexBufferSize == 0 ? indexBufferSize : newIndexBufferSize;
                indexBuffer = device.CreateBuffer(new BufferDescription(indexBufferSize * sizeof(ushort), BindFlags.IndexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));
            }

            var vertexResource = context.Map(vertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            var indexResource = context.Map(indexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            var vertexResourcePointer = (DebugDrawVert*)vertexResource.DataPointer;
            var indexResourcePointer = (ushort*)indexResource.DataPointer;

            for (int i = 0; i < queue.Commands.Count; i++)
            {
                var cmd = queue.Commands[i];
                MemcpyT(cmd.Vertices, vertexResourcePointer, cmd.nVertices);
                MemcpyT(cmd.Indices, indexResourcePointer, cmd.nIndices);
                vertexResourcePointer += cmd.nVertices;
                indexResourcePointer += cmd.nIndices;
            }

            context.Unmap(vertexBuffer, 0);
            context.Unmap(indexBuffer, 0);

            {
                var mappedResource = context.Map(constantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
                Matrix4x4 mvp = Matrix4x4.Transpose(camera);
                Buffer.MemoryCopy(&mvp, (void*)mappedResource.DataPointer, mappedResource.RowPitch, sizeof(Matrix4x4));
                context.Unmap(constantBuffer, 0);
            }

            // Setup desired state
            SetupRenderState(DebugDraw.GetViewport(), context);

            int voffset = 0;
            uint ioffset = 0;
            bool depthWasEnabled = false;
            context.OMSetRenderTargets(rtv, null);
            for (int i = 0; i < queue.Commands.Count; i++)
            {
                DebugDrawCommand cmd = queue.Commands[i];

                if (cmd.EnableDepth && !depthWasEnabled)
                {
                    context.OMSetRenderTargets(rtv, dsv);
                    depthWasEnabled = true;
                }
                else if (depthWasEnabled)
                {
                    context.OMSetRenderTargets(rtv, null);
                    depthWasEnabled = false;
                }
                context.IASetPrimitiveTopology(cmd.Topology);
                context.DrawIndexedInstanced((int)cmd.nIndices, 1, (int)ioffset, voffset, 0);
                voffset += (int)cmd.nVertices;
                ioffset += cmd.nIndices;
            }

            context.VSSetShader(null);
            context.PSSetShader(null);
            context.IASetInputLayout(null);
            context.RSSetState(null);
            context.OMSetDepthStencilState(null);
            context.OMSetBlendState(null);
            context.RSSetViewport(default);
            context.IASetVertexBuffer(0, null, 0, 0);
            context.IASetIndexBuffer(null, default, 0);
            context.IASetPrimitiveTopology(PrimitiveTopology.Undefined);
            context.VSSetConstantBuffer(0, null);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                indexBuffer?.Dispose();
                vertexBuffer?.Dispose();
                blendState.Dispose();
                depthStencilState.Dispose();
                rasterizerState.Dispose();
                pixelShader.Dispose();
                pixelShaderBlob.Dispose();
                constantBuffer.Dispose();
                inputLayout.Dispose();
                vertexShader.Dispose();
                vertexShaderBlob.Dispose();
                disposedValue = true;
            }
        }

        ~DebugDrawD3D11Renderer()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}