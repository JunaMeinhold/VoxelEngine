namespace VoxelEngine.UI
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.D3DCompiler;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics.D3D11;

    public unsafe class DebugDrawD3D11Renderer : IDisposable
    {
        private static ComPtr<ID3D11Device> device;
        private static ComPtr<ID3D11DeviceContext> context;
        private static ComPtr<ID3D11VertexShader> vertexShader;
        private static ComPtr<ID3D11PixelShader> pixelShader;
        private static ComPtr<ID3D11DepthStencilState> depthStencilState;
        private static ComPtr<ID3D11BlendState> blendState;
        private static ComPtr<ID3D11RasterizerState> rasterizerState;
        private static ComPtr<ID3D11InputLayout> inputLayout;
        private static ComPtr<ID3D11Buffer> vertexBuffer;
        private static ComPtr<ID3D11Buffer> indexBuffer;
        private static ComPtr<ID3D11Buffer> constantBuffer;

        private int vertexBufferSize = 5000;
        private int indexBufferSize = 10000;

        public DebugDrawD3D11Renderer(ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context)
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

            byte* pVertexShaderCode = vertexShaderCode.ToUTF8Ptr();
            int lengthVs = StrLen(pVertexShaderCode);
            ComPtr<ID3D10Blob> errorBlob = default;
            ComPtr<ID3D10Blob> vertexShaderBlob = default;

            D3DCompiler.Compile(pVertexShaderCode, (nuint)lengthVs, "vs", (Hexa.NET.D3DCommon.ShaderMacro*)null, (ID3DInclude*)null, "main", "vs_4_0", 0, 0, vertexShaderBlob.GetAddressOf(), errorBlob.GetAddressOf());
            Free(pVertexShaderCode);

            if (errorBlob.Handle != null)
            {
                var errorString = ToStringFromUTF8((byte*)errorBlob.GetBufferPointer());
                Debug.WriteLine(errorString);
                errorBlob.Release();
                errorBlob = default;
            }

            if (vertexShaderBlob.Handle == null)
            {
                throw new Exception("error compiling vertex shader");
            }

            ComPtr<ID3D11VertexShader> vtxShader = default;
            device.CreateVertexShader(vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize(), (ID3D11ClassLinkage*)null, vtxShader.GetAddressOf());
            vertexShader = vtxShader;

            InputElementDesc* inputElements = stackalloc InputElementDesc[]
            {
                new InputElementDesc( "POSITION".ToUTF8Ptr(), 0, Format.R32G32B32Float,0,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDesc( "TEXCOORD".ToUTF8Ptr(), 0, Format.R32G32Float,   12, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDesc( "COLOR".ToUTF8Ptr(),    0, Format.R8G8B8A8Unorm, 20, 0, InputClassification.PerVertexData, 0 ),
            };

            device.CreateInputLayout(inputElements, 3, vertexShaderBlob.GetBufferPointer(), vertexShaderBlob.GetBufferSize(), inputLayout.GetAddressOf());

            for (int i = 0; i < 3; i++)
            {
                Free(inputElements[i].SemanticName);
            }

            vertexShaderBlob.Release();

            BufferDesc constBufferDesc = new()
            {
                ByteWidth = (uint)sizeof(Matrix4x4),
                Usage = Usage.Dynamic,
                BindFlags = (uint)BindFlag.ConstantBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
            };
            device.CreateBuffer(ref constBufferDesc, null, out constantBuffer);

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
            ComPtr<ID3D10Blob> pixelShaderBlob = default;

            byte* pPixelShaderCode = pixelShaderCode.ToUTF8Ptr();
            int lengthPx = StrLen(pPixelShaderCode);
            D3DCompiler.Compile(pPixelShaderCode, (nuint)lengthPx, "ps", (Hexa.NET.D3DCommon.ShaderMacro*)null, (ID3DInclude*)null, "main", "ps_4_0", 0, 0, pixelShaderBlob.GetAddressOf(), errorBlob.GetAddressOf());
            Free(pPixelShaderCode);

            if (errorBlob.Handle != null)
            {
                var errorString = ToStringFromUTF8((byte*)errorBlob.GetBufferPointer());
                Debug.WriteLine(errorString);
                errorBlob.Release();
                errorBlob = default;
            }

            if (pixelShaderBlob.Handle == null)
            {
                throw new Exception("error compiling pixel shader");
            }

            ComPtr<ID3D11PixelShader> pxShader = default;
            device.CreatePixelShader(pixelShaderBlob.GetBufferPointer(), pixelShaderBlob.GetBufferSize(), (ID3D11ClassLinkage*)null, pxShader.GetAddressOf());
            pixelShader = pxShader;

            pixelShaderBlob.Release();

            BlendDesc blendDesc = new()
            {
                AlphaToCoverageEnable = false
            };

            blendDesc.RenderTarget[0] = new RenderTargetBlendDesc
            {
                BlendOpAlpha = BlendOp.Add,
                BlendEnable = true,
                BlendOp = BlendOp.Add,
                DestBlendAlpha = Blend.InvSrcAlpha,
                DestBlend = Blend.InvSrcAlpha,
                SrcBlend = Blend.SrcAlpha,
                SrcBlendAlpha = Blend.SrcAlpha,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All
            };

            device.CreateBlendState(ref blendDesc, out blendState);

            RasterizerDesc rasterDesc = new()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                ScissorEnable = false,
                DepthClipEnable = true,
                AntialiasedLineEnable = true,
                MultisampleEnable = false
            };

            device.CreateRasterizerState(ref rasterDesc, out rasterizerState);

            DepthStencilopDesc stencilOpDesc = new(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep, ComparisonFunc.Always);
            DepthStencilDesc depthDesc = new()
            {
                DepthEnable = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunc.LessEqual,
                StencilEnable = false,
                FrontFace = stencilOpDesc,
                BackFace = stencilOpDesc
            };

            device.CreateDepthStencilState(ref depthDesc, out depthStencilState);
        }

        public void BeginDraw()
        {
            DebugDraw.NewFrame();
        }

        public void EndDraw(ComPtr<ID3D11RenderTargetView> rtv, ComPtr<ID3D11DepthStencilView> dsv)
        {
            DebugDraw.Render();
            Render(DebugDraw.GetQueue(), DebugDraw.GetCamera(), rtv, dsv);
        }

        private static unsafe void SetupRenderState(Hexa.NET.Mathematics.Viewport drawData, ComPtr<ID3D11DeviceContext> ctx)
        {
            var viewport = drawData;

            uint stride = (uint)sizeof(DebugDrawVert);
            uint offset = 0;

            ctx.VSSetShader(vertexShader, (ID3D11ClassInstance*)null, 0);
            ctx.PSSetShader(pixelShader, (ID3D11ClassInstance*)null, 0);
            ctx.IASetInputLayout(inputLayout);
            ctx.RSSetState(rasterizerState);
            ctx.OMSetDepthStencilState(depthStencilState, 0);
            ctx.OMSetBlendState(blendState, null, 0);
            ctx.RSSetViewport(viewport);
            var vtxBuffer = vertexBuffer.Handle;
            ctx.IASetVertexBuffers(0, 1, &vtxBuffer, &stride, &offset);
            ctx.IASetIndexBuffer(indexBuffer, sizeof(ushort) == 2 ? Format.R16Uint : Format.R32Uint, 0);
            ctx.IASetPrimitiveTopology(PrimitiveTopology.Trianglelist);
            var cb = constantBuffer.Handle;
            ctx.VSSetConstantBuffers(0, 1, &cb);
        }

        private void Render(DebugDrawCommandQueue queue, Matrix4x4 camera, ComPtr<ID3D11RenderTargetView> rtv, ComPtr<ID3D11DepthStencilView> dsv)
        {
            if (queue.VertexCount > vertexBufferSize || vertexBuffer.Handle == null)
            {
                if (vertexBuffer.Handle != null)
                {
                    vertexBuffer.Release();
                }

                vertexBuffer.Release();
                var newVertexBufferSize = (int)(queue.VertexCount * 1.5f);
                vertexBufferSize = newVertexBufferSize == 0 ? vertexBufferSize : newVertexBufferSize;
                BufferDesc desc = new((uint)(vertexBufferSize * sizeof(DebugDrawVert)), Usage.Dynamic, (uint)BindFlag.VertexBuffer, (uint)CpuAccessFlag.Write);
                device.CreateBuffer(ref desc, null, out vertexBuffer);
            }

            if (queue.IndexCount > indexBufferSize || indexBuffer.Handle == null)
            {
                if (indexBuffer.Handle != null)
                {
                    indexBuffer.Release();
                }

                var newIndexBufferSize = (int)(queue.IndexCount * 1.5f);
                indexBufferSize = newIndexBufferSize == 0 ? indexBufferSize : newIndexBufferSize;
                BufferDesc desc = new((uint)(indexBufferSize * sizeof(ushort)), Usage.Dynamic, (uint)BindFlag.IndexBuffer, (uint)CpuAccessFlag.Write);
                device.CreateBuffer(ref desc, null, out indexBuffer);
            }

            MappedSubresource vertexResource;
            MappedSubresource indexResource;
            context.Map(vertexBuffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &vertexResource);
            context.Map(indexBuffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &indexResource);
            var vertexResourcePointer = (DebugDrawVert*)vertexResource.PData;
            var indexResourcePointer = (ushort*)indexResource.PData;

            for (int i = 0; i < queue.Commands.Count; i++)
            {
                var cmd = queue.Commands[i];
                MemcpyT(cmd.Vertices, vertexResourcePointer, cmd.nVertices);
                MemcpyT(cmd.Indices, indexResourcePointer, cmd.nIndices);
                vertexResourcePointer += cmd.nVertices;
                indexResourcePointer += cmd.nIndices;
            }

            context.Unmap(vertexBuffer.As<ID3D11Resource>(), 0);
            context.Unmap(indexBuffer.As<ID3D11Resource>(), 0);

            {
                MappedSubresource mappedResource;
                context.Map(constantBuffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &mappedResource);
                Matrix4x4 mvp = Matrix4x4.Transpose(camera);
                Buffer.MemoryCopy(&mvp, mappedResource.PData, mappedResource.RowPitch, sizeof(Matrix4x4));
                context.Unmap(constantBuffer.As<ID3D11Resource>(), 0);
            }

            // Setup desired state
            SetupRenderState(DebugDraw.GetViewport(), context);

            int voffset = 0;
            uint ioffset = 0;
            bool depthWasEnabled = false;
            context.OMSetRenderTargets(1, &rtv.Handle, (ID3D11DepthStencilView*)null);
            for (int i = 0; i < queue.Commands.Count; i++)
            {
                DebugDrawCommand cmd = queue.Commands[i];

                if (cmd.EnableDepth && !depthWasEnabled)
                {
                    context.OMSetRenderTargets(1, &rtv.Handle, dsv);
                    depthWasEnabled = true;
                }
                else if (depthWasEnabled)
                {
                    context.OMSetRenderTargets(1, &rtv.Handle, (ID3D11DepthStencilView*)null);
                    depthWasEnabled = false;
                }
                context.IASetPrimitiveTopology(cmd.Topology);
                context.DrawIndexedInstanced((uint)(int)cmd.nIndices, 1, (uint)(int)ioffset, voffset, 0);
                voffset += (int)cmd.nVertices;
                ioffset += cmd.nIndices;
            }

            context.VSSetShader(null, (ID3D11ClassInstance*)null, 0);
            context.PSSetShader(null, (ID3D11ClassInstance*)null, 0);
            context.IASetInputLayout(null);
            context.RSSetState((ID3D11RasterizerState*)null);
            context.OMSetDepthStencilState((ID3D11DepthStencilState*)null, 0);
            context.OMSetBlendState((ID3D11BlendState*)null, (float*)null, 0);
            context.RSSetViewport(default(Viewport));
            void* nullPtr = null;
            uint stride = 0, offset = 0;
            context.IASetVertexBuffers(0, 1, (ID3D11Buffer**)&nullPtr, &stride, &offset);
            context.IASetIndexBuffer((ID3D11Buffer*)null, default, 0);
            context.IASetPrimitiveTopology(PrimitiveTopology.Undefined);
            context.VSSetConstantBuffers(0, 1, (ID3D11Buffer**)&nullPtr);
        }

        protected virtual void DisposeCore()
        {
            if (indexBuffer.Handle != null)
            {
                indexBuffer.Release();
                indexBuffer = null;
            }
            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
                vertexBuffer = null;
            }
            if (blendState.Handle != null)
            {
                blendState.Release();
                blendState = null;
            }
            if (depthStencilState.Handle != null)
            {
                depthStencilState.Release();
                depthStencilState = null;
            }
            if (rasterizerState.Handle != null)
            {
                rasterizerState.Release();
                rasterizerState = null;
            }
            if (pixelShader.Handle != null)
            {
                pixelShader.Release();
                pixelShader = null;
            }
            if (constantBuffer.Handle != null)
            {
                constantBuffer.Release();
                constantBuffer = null;
            }
            if (inputLayout.Handle != null)
            {
                inputLayout.Release();
                inputLayout = null;
            }
            if (vertexShader.Handle != null)
            {
                vertexShader.Release();
                vertexShader = null;
            }
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }
}