//based on https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
#nullable disable

using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using VoxelEngine.Core;
using VoxelEngine.Debugging;
using VoxelEngine.Mathematics;
using VoxelEngine.Rendering.D3D;
using VoxelEngine.Rendering.DXGI;
using ImDrawIdx = System.UInt16;
using MapFlags = Vortice.Direct3D11.MapFlags;

namespace VoxelEngine.UI
{
    public unsafe class ImGuiRenderer
    {
        private const int VertexConstantBufferSize = 16 * 4;

        private ID3D11Device device;
        private ID3D11DeviceContext context;
        private ImGuiInputHandler inputHandler;
        private ID3D11Buffer vertexBuffer;
        private ID3D11Buffer indexBuffer;
        private Blob vertexShaderBlob;
        private ID3D11VertexShader vertexShader;
        private ID3D11InputLayout inputLayout;
        private ID3D11Buffer constantBuffer;
        private Blob pixelShaderBlob;
        private ID3D11PixelShader pixelShader;
        private ID3D11SamplerState fontSampler;
        private ID3D11ShaderResourceView fontTextureView;
        private ID3D11RasterizerState rasterizerState;
        private ID3D11BlendState blendState;
        private ID3D11DepthStencilState depthStencilState;
        private int vertexBufferSize = 5000, indexBufferSize = 10000;

        private static readonly Dictionary<IntPtr, ID3D11ShaderResourceView> textureResources = new();

        public ImGuiRenderer(Window window)
        {
            IntPtr igContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(igContext);

            device = D3D11DeviceManager.ID3D11Device;
            context = D3D11DeviceManager.ID3D11DeviceContext;

            ImGuiIOPtr io = ImGui.GetIO();
            ImFontConfigPtr config = new(ImGuiNative.ImFontConfig_ImFontConfig());

            io.Fonts.AddFontDefault(config);
            config.MergeMode = true;
            config.GlyphMinAdvanceX = 18;
            config.GlyphOffset = new(0, 4);
            char[] range = new char[] { (char)0xE700, (char)0xF800, (char)0 };
            fixed (char* buffer = range)
            {
                io.Fonts.AddFontFromFileTTF("C:\\windows\\fonts\\SegoeIcons.ttf", 14, config, (IntPtr)buffer);
            }

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.HasMouseCursors;
            ImGui.StyleColorsDark();
            CreateDeviceObjects();

            RangeAccessor<Vector4> colors = ImGui.GetStyle().Colors;
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.48f, 0.16f, 0.44f, 0.54f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.98f, 0.26f, 0.95f, 0.40f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.86f, 0.26f, 0.98f, 0.67f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.45f, 0.16f, 0.48f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.94f, 0.26f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.80f, 0.24f, 0.88f, 1.00f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.96f, 0.26f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.98f, 0.26f, 0.95f, 0.40f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.96f, 0.26f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.89f, 0.06f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.92f, 0.26f, 0.98f, 0.31f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.92f, 0.26f, 0.98f, 0.80f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.92f, 0.26f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.70f, 0.10f, 0.75f, 0.78f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.70f, 0.10f, 0.75f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.92f, 0.26f, 0.98f, 0.20f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.92f, 0.26f, 0.98f, 0.67f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.92f, 0.26f, 0.98f, 0.95f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.55f, 0.18f, 0.58f, 0.86f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.92f, 0.26f, 0.98f, 0.80f);
            colors[(int)ImGuiCol.TabActive] = new Vector4(0.64f, 0.20f, 0.68f, 1.00f);
            colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.14f, 0.07f, 0.15f, 0.97f);
            colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.40f, 0.14f, 0.42f, 1.00f);
            colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.92f, 0.26f, 0.98f, 0.70f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.92f, 0.26f, 0.98f, 0.35f);
            colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.92f, 0.26f, 0.98f, 1.00f);

            inputHandler = new(window);
        }

        public bool NoInternal;

        public void BeginDraw()
        {
            inputHandler.Update();
            ImGui.NewFrame();

            ImGuiConsole.Draw();
        }

        public void EndDraw()
        {
            ImGui.Render();
            ImGui.EndFrame();
            DXGIDeviceManager.SwapChain.SetTarget(context);
            Render(ImGui.GetDrawData());
        }

        public void Render(ImDrawDataPtr data)
        {
            // Avoid rendering when minimized
            if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            {
                return;
            }

            if (data.CmdListsCount == 0)
            {
                return;
            }

            ID3D11DeviceContext ctx = context;

            if (vertexBuffer == null || vertexBufferSize < data.TotalVtxCount)
            {
                vertexBuffer?.Dispose();

                vertexBufferSize = (int)(data.TotalVtxCount * 1.5f);
                BufferDescription desc = new();
                desc.Usage = ResourceUsage.Dynamic;
                desc.ByteWidth = vertexBufferSize * sizeof(ImDrawVert);
                desc.BindFlags = BindFlags.VertexBuffer;
                desc.CPUAccessFlags = CpuAccessFlags.Write;
                vertexBuffer = device.CreateBuffer(desc);
            }

            if (indexBuffer == null || indexBufferSize < data.TotalIdxCount)
            {
                indexBuffer?.Dispose();

                indexBufferSize = (int)(data.TotalIdxCount * 1.5f);

                BufferDescription desc = new();
                desc.Usage = ResourceUsage.Dynamic;
                desc.BindFlags = BindFlags.IndexBuffer;
                desc.ByteWidth = indexBufferSize * sizeof(ImDrawIdx);
                desc.BindFlags = BindFlags.IndexBuffer;
                desc.CPUAccessFlags = CpuAccessFlags.Write;
                indexBuffer = device.CreateBuffer(desc);
            }

            // Upload vertex/index data into a single contiguous GPU buffer
            MappedSubresource vertexResource = ctx.Map(vertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            MappedSubresource indexResource = ctx.Map(indexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            ImDrawVert* vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
            ushort* indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
            for (int n = 0; n < data.CmdListsCount; n++)
            {
                ImDrawListPtr cmdlList = data.CmdListsRange[n];

                int vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
                Buffer.MemoryCopy((void*)cmdlList.VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

                int idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
                Buffer.MemoryCopy((void*)cmdlList.IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

                vertexResourcePointer += cmdlList.VtxBuffer.Size;
                indexResourcePointer += cmdlList.IdxBuffer.Size;
            }
            ctx.Unmap(vertexBuffer, 0);
            ctx.Unmap(indexBuffer, 0);

            // Setup orthographic projection matrix into our constant buffer
            // Our visible imgui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.

            MappedSubresource constResource = ctx.Map(constantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            Span<byte> span = constResource.AsSpan<byte>(VertexConstantBufferSize);
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = MathUtil.OrthoOffCenterLH(0f, io.DisplaySize.X, io.DisplaySize.Y, 0, -1, 1);
            MemoryMarshal.Write(span, ref mvp);
            ctx.Unmap(constantBuffer, 0);

            ID3D11RasterizerState rsBefore = ctx.RSGetState();
            ctx.OMGetDepthStencilState(out ID3D11DepthStencilState dsBefore, out int stencilRefBefore);
            ID3D11BlendState bsBefore = ctx.OMGetBlendState();
            SetupRenderState(data, ctx);

            data.ScaleClipRects(io.DisplayFramebufferScale);

            // Render command lists
            // (Because we merged all buffers into a single one, we maintain our own offset into them)
            int vtx_offset = 0;
            int idx_offset = 0;

            for (int n = 0; n < data.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = data.CmdListsRange[n];

                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    ImDrawCmdPtr cmd = cmdList.CmdBuffer[i];
                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException("user callbacks not implemented");
                    }
                    else
                    {
                        ctx.RSSetScissorRect((int)cmd.ClipRect.X, (int)cmd.ClipRect.Y, (int)cmd.ClipRect.Z, (int)cmd.ClipRect.W);

                        textureResources.TryGetValue(cmd.TextureId, out ID3D11ShaderResourceView texture);
                        if (texture != null)
                        {
                            ctx.PSSetShaderResource(0, texture);
                        }

                        ctx.DrawIndexed((int)cmd.ElemCount, idx_offset, vtx_offset);
                    }
                    idx_offset += (int)cmd.ElemCount;
                }
                vtx_offset += cmdList.VtxBuffer.Size;
            }

            ctx.ClearState();
            ctx.RSSetState(rsBefore);
            ctx.OMSetDepthStencilState(dsBefore, stencilRefBefore);
            ctx.OMSetBlendState(bsBefore);
        }

        public void Dispose()
        {
            if (device == null)
            {
                return;
            }

            InvalidateDeviceObjects();
        }

        private void SetupRenderState(ImDrawDataPtr drawData, ID3D11DeviceContext ctx)
        {
            ctx.RSSetViewport(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y);

            int stride = sizeof(ImDrawVert);
            int offset = 0;
            ctx.IASetInputLayout(inputLayout);
            ctx.IASetVertexBuffer(0, vertexBuffer, stride, offset);
            ctx.IASetIndexBuffer(indexBuffer, sizeof(ushort) == 2 ? Format.R16_UInt : Format.R32_UInt, 0);
            ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            ctx.VSSetShader(vertexShader);
            ctx.VSSetConstantBuffer(0, constantBuffer);
            ctx.PSSetShader(pixelShader);
            ctx.PSSetSampler(0, fontSampler);
            ctx.GSSetShader(null);
            ctx.HSSetShader(null);
            ctx.DSSetShader(null);
            ctx.CSSetShader(null);

            ctx.OMSetBlendState(blendState);
            ctx.OMSetDepthStencilState(depthStencilState);
            ctx.RSSetState(rasterizerState);
        }

        private void CreateFontsTexture()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            byte* pixels;
            int width, height;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

            Texture2DDescription texDesc = new()
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription { Count = 1 },
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None
            };

            SubresourceData subResource = new()
            {
                DataPointer = (IntPtr)pixels,
                RowPitch = texDesc.Width * 4,
                SlicePitch = 0
            };

            ID3D11Texture2D texture = device.CreateTexture2D(texDesc, new[] { subResource });

            ShaderResourceViewDescription resViewDesc = new()
            {
                Format = Format.R8G8B8A8_UNorm,
                ViewDimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new Texture2DShaderResourceView { MipLevels = texDesc.MipLevels, MostDetailedMip = 0 }
            };
            fontTextureView = device.CreateShaderResourceView(texture, resViewDesc);
            texture.Dispose();

            io.Fonts.TexID = RegisterTexture(fontTextureView);

            SamplerDescription samplerDesc = new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0f,
                ComparisonFunction = ComparisonFunction.Always,
                MinLOD = 0f,
                MaxLOD = 0f
            };
            fontSampler = device.CreateSamplerState(samplerDesc);
        }

        public static IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
        {
            IntPtr imguiID = texture.NativePointer;
            textureResources.Add(imguiID, texture);
            return imguiID;
        }

        public static IntPtr TryRegisterTexture(ID3D11ShaderResourceView texture)
        {
            IntPtr imguiID = texture.NativePointer;
            if (!textureResources.ContainsKey(imguiID))
            {
                textureResources.Add(imguiID, texture);
            }

            return imguiID;
        }

        public static void UnregisterTexture(ID3D11ShaderResourceView texture)
        {
            textureResources.Remove(texture.NativePointer);
        }

        private void CreateDeviceObjects()
        {
            string vertexShaderCode =
                @"
                    cbuffer vertexBuffer : register(b0)
                    {
                        float4x4 ProjectionMatrix;
                    };

                    struct VS_INPUT
                    {
                        float2 pos : POSITION;
                        float2 uv  : TEXCOORD0;
                        float4 col : COLOR0;
                    };

                    struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    PS_INPUT main(VS_INPUT input)
                    {
                        PS_INPUT output;
                        output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
                        output.col = input.col;
                        output.uv  = input.uv;
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
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
            };

            inputLayout = device.CreateInputLayout(inputElements, vertexShaderBlob);

            BufferDescription constBufferDesc = new()
            {
                ByteWidth = VertexConstantBufferSize,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
            };
            constantBuffer = device.CreateBuffer(constBufferDesc);

            string pixelShaderCode =
                @"struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    sampler sampler0;
                    Texture2D texture0;

                    float4 main(PS_INPUT input) : SV_Target
                    {
                        float4 out_col = input.col * texture0.Sample(sampler0, input.uv);
                        return out_col;
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
                ScissorEnable = true,
                DepthClipEnable = false,
            };

            rasterizerState = device.CreateRasterizerState(rasterDesc);

            DepthStencilOperationDescription stencilOpDesc = new(StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, ComparisonFunction.Never);
            DepthStencilDescription depthDesc = new()
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.Zero,
                DepthFunc = ComparisonFunction.Always,
                StencilEnable = false,
                FrontFace = stencilOpDesc,
                BackFace = stencilOpDesc
            };

            depthStencilState = device.CreateDepthStencilState(depthDesc);

            CreateFontsTexture();
        }

        private void InvalidateDeviceObjects()
        {
            fontSampler.Dispose();
            fontTextureView.Dispose();
            indexBuffer.Dispose();
            vertexBuffer.Dispose();
            blendState.Dispose();
            depthStencilState.Dispose();
            rasterizerState.Dispose();
            pixelShader.Dispose();
            pixelShaderBlob.Dispose();
            constantBuffer.Dispose();
            inputLayout.Dispose();
            vertexShader.Dispose();
            vertexShaderBlob.Dispose();
        }
    }
}