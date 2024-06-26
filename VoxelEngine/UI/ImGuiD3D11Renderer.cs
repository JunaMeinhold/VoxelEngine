﻿//based on https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
#nullable disable

using Silk.NET.Direct3D11;

namespace HexaEngine.Rendering.Renderers
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using HexaEngine.Core.Unsafes;
    using Hexa.NET.ImGui;
    using Vortice.DXGI;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;
    using ImDrawIdx = UInt16;
    using MapFlags = Vortice.Direct3D11.MapFlags;
    using VoxelEngine.Rendering.DXGI;
    using VoxelEngine.Rendering.D3D;
    using Vortice.D3DCompiler;

    public static class ImGuiD3D11Renderer
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
        private static ID3D11SamplerState fontSampler;
        private static ID3D11ShaderResourceView fontTextureView;
        private static int vertexBufferSize = 5000, indexBufferSize = 10000;

        /// <summary>
        /// Renderer data
        /// </summary>
        private struct RendererData
        {
            public int Dummy;
        }

        // Backend data stored in io.BackendRendererUserData to allow support for multiple Dear ImGui contexts
        // It is STRONGLY preferred that you use docking branch with multi-viewports (== single Dear ImGui context + multiple windows) instead of multiple Dear ImGui contexts.
        private static unsafe RendererData* GetBackendData()
        {
            return !ImGui.GetCurrentContext().IsNull ? (RendererData*)ImGui.GetIO().BackendRendererUserData : null;
        }

        private static unsafe void SetupRenderState(ImDrawData* drawData, ID3D11DeviceContext ctx)
        {
            var viewport = new Viewport(drawData->DisplaySize.X, drawData->DisplaySize.Y);

            uint stride = (uint)sizeof(ImDrawVert);
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
            ctx.PSSetSampler(0, fontSampler);
        }

        /// <summary>
        /// Render function
        /// </summary>
        /// <param name="data"></param>
        public static unsafe void RenderDrawData(ImDrawData* data)
        {
            // Avoid rendering when minimized
            if (data->DisplaySize.X <= 0.0f || data->DisplaySize.Y <= 0.0f)
            {
                return;
            }

            if (data->CmdListsCount == 0)
            {
                return;
            }

            ID3D11DeviceContext ctx = context;

            // Create and grow vertex/index buffers if needed
            if (vertexBuffer == null || vertexBufferSize < data->TotalVtxCount)
            {
                vertexBuffer?.Dispose();
                vertexBufferSize = data->TotalVtxCount + 5000;
                BufferDescription desc = new();
                desc.Usage = ResourceUsage.Dynamic;
                desc.ByteWidth = vertexBufferSize * sizeof(ImDrawVert);
                desc.BindFlags = BindFlags.VertexBuffer;
                desc.CPUAccessFlags = CpuAccessFlags.Write;
                vertexBuffer = device.CreateBuffer(desc);
            }

            if (indexBuffer == null || indexBufferSize < data->TotalIdxCount)
            {
                indexBuffer?.Dispose();
                indexBufferSize = data->TotalIdxCount + 10000;
                BufferDescription desc = new();
                desc.Usage = ResourceUsage.Dynamic;
                desc.ByteWidth = indexBufferSize * sizeof(ImDrawIdx);
                desc.BindFlags = BindFlags.IndexBuffer;
                desc.CPUAccessFlags = CpuAccessFlags.Write;
                indexBuffer = device.CreateBuffer(desc);
            }

            // Upload vertex/index data into a single contiguous GPU buffer
            var vertexResource = ctx.Map(vertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            var indexResource = ctx.Map(indexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            var vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
            var indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
            for (int n = 0; n < data->CmdListsCount; n++)
            {
                var cmdlList = data->CmdLists.Data[n];

                var vertBytes = cmdlList->VtxBuffer.Size * sizeof(ImDrawVert);
                Buffer.MemoryCopy(cmdlList->VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

                var idxBytes = cmdlList->IdxBuffer.Size * sizeof(ImDrawIdx);
                Buffer.MemoryCopy(cmdlList->IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

                vertexResourcePointer += cmdlList->VtxBuffer.Size;
                indexResourcePointer += cmdlList->IdxBuffer.Size;
            }
            ctx.Unmap(vertexBuffer, 0);
            ctx.Unmap(indexBuffer, 0);

            // Setup orthographic projection matrix into our constant buffer
            // Our visible imgui space lies from draw_data->DisplayPos (top left) to draw_data->DisplayPos+data_data->DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
            {
                var mappedResource = ctx.Map(constantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
                Matrix4x4* constant_buffer = (Matrix4x4*)mappedResource.DataPointer;

                float L = data->DisplayPos.X;
                float R = data->DisplayPos.X + data->DisplaySize.X;
                float T = data->DisplayPos.Y;
                float B = data->DisplayPos.Y + data->DisplaySize.Y;
                Matrix4x4 mvp = new
                    (
                     2.0f / (R - L), 0.0f, 0.0f, 0.0f,
                     0.0f, 2.0f / (T - B), 0.0f, 0.0f,
                     0.0f, 0.0f, 0.5f, 0.0f,
                     (R + L) / (L - R), (T + B) / (B - T), 0.5f, 1.0f
                     );
                Buffer.MemoryCopy(&mvp, constant_buffer, sizeof(Matrix4x4), sizeof(Matrix4x4));
                ctx.Unmap(constantBuffer, 0);
            }

            // Setup desired state
            SetupRenderState(data, ctx);

            // Render command lists
            // (Because we merged all buffers into a single one, we maintain our own offset into them)
            int global_idx_offset = 0;
            int global_vtx_offset = 0;
            Vector2 clip_off = data->DisplayPos;
            for (int n = 0; n < data->CmdListsCount; n++)
            {
                var cmdList = data->CmdLists.Data[n];

                for (int i = 0; i < cmdList->CmdBuffer.Size; i++)
                {
                    var cmd = cmdList->CmdBuffer.Data[i];
                    if (cmd.UserCallback != null)
                    {
                        // User callback, registered via ImDrawList::AddCallback()
                        // (ImDrawCallback_ResetRenderState is a special callback value used by the user to request the renderer to reset render state.)
                        if ((nint)cmd.UserCallback == -1)
                        {
                            SetupRenderState(data, ctx);
                        }
                        else
                        {
                            Marshal.GetDelegateForFunctionPointer<UserCallback>((nint)cmd.UserCallback)(cmdList, &cmd);
                        }
                    }
                    else
                    {
                        // Project scissor/clipping rectangles into framebuffer space
                        Vector2 clip_min = new(cmd.ClipRect.X - clip_off.X, cmd.ClipRect.Y - clip_off.Y);
                        Vector2 clip_max = new(cmd.ClipRect.Z - clip_off.X, cmd.ClipRect.W - clip_off.Y);
                        if (clip_max.X <= clip_min.X || clip_max.Y <= clip_min.Y)
                            continue;

                        // Apply scissor/clipping rectangle
                        ctx.RSSetScissorRect((int)clip_min.X, (int)clip_min.Y, (int)clip_max.X, (int)clip_max.Y);

                        // Bind texture, Draw
                        var srv = (void*)cmd.TextureId.Handle;
                        ctx.PSSetShaderResource(0, new ID3D11ShaderResourceView((nint)srv));
                        ctx.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)(uint)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset), 0);
                    }
                }
                global_idx_offset += cmdList->IdxBuffer.Size;
                global_vtx_offset += cmdList->VtxBuffer.Size;
            }

            ctx.VSSetShader(null);
            ctx.PSSetShader(null);
            ctx.IASetInputLayout(null);
            ctx.RSSetState(null);
            ctx.OMSetDepthStencilState(null);
            ctx.OMSetBlendState(null);
            ctx.RSSetViewport(default);
            ctx.IASetVertexBuffer(0, null, 0, 0);
            ctx.IASetIndexBuffer(null, default, 0);
            ctx.IASetPrimitiveTopology(PrimitiveTopology.Undefined);
            ctx.VSSetConstantBuffer(0, null);
            ctx.PSSetSampler(0, null);
            ctx.PSSetShaderResource(0, null);
        }

        private static unsafe void CreateFontsTexture()
        {
            var io = ImGui.GetIO();
            byte* pixels;
            int width;
            int height;
            ImGui.GetTexDataAsRGBA32(io.Fonts, &pixels, &width, &height, null);

            var texDesc = new Texture2DDescription
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

            var subResource = new SubresourceData
            {
                DataPointer = (nint)pixels,
                RowPitch = texDesc.Width * 4,
                SlicePitch = 0
            };

            var texture = device.CreateTexture2D(texDesc, new[] { subResource });

            var resViewDesc = new ShaderResourceViewDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                ViewDimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new Texture2DShaderResourceView { MipLevels = texDesc.MipLevels, MostDetailedMip = 0 }
            };
            fontTextureView = device.CreateShaderResourceView(texture, resViewDesc);
            texture.Dispose();

            io.Fonts.TexID = fontTextureView.NativePointer;

            var samplerDesc = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0f,
                ComparisonFunc = ComparisonFunction.Always,
                MinLOD = 0f,
                MaxLOD = 0f
            };
            fontSampler = device.CreateSamplerState(samplerDesc);
        }

        private static unsafe void CreateDeviceObjects()
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
                ByteWidth = sizeof(Matrix4x4),
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
                BlendEnable = true,
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

        private static void InvalidateDeviceObjects()
        {
            fontSampler.Dispose();
            fontTextureView.Dispose();
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
        }

        public static unsafe bool Init(ID3D11Device device, ID3D11DeviceContext context)
        {
            var io = ImGui.GetIO();
            Trace.Assert(io.BackendRendererUserData == null, "Already initialized a renderer backend!");

            // Setup backend capabilities flags
            var bd = AllocT<RendererData>();
            io.BackendRendererUserData = bd;
            io.BackendRendererName = "ImGui_Generic_Renderer".ToUTF8();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset; // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.
            io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports; // We can create multi-viewports on the Renderer side (optional)

            ImGuiD3D11Renderer.device = device;
            ImGuiD3D11Renderer.context = context;

            CreateDeviceObjects();

            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                InitPlatformInterface();

            return true;
        }

        public static unsafe void Shutdown()
        {
            RendererData* bd = GetBackendData();
            Trace.Assert(bd != null, "No renderer backend to shutdown, or already shutdown?");
            var io = ImGui.GetIO();

            ShutdownPlatformInterface();
            InvalidateDeviceObjects();

            io.BackendRendererName = null;
            io.BackendRendererUserData = null;
            io.BackendFlags &= ~(ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasViewports);
            Free(bd);
        }

        public static unsafe void NewFrame()
        {
            RendererData* bd = GetBackendData();
            Trace.Assert(bd != null, "Did you call ImGui_ImplDX11_Init()?");

            if (fontSampler == null)
                CreateDeviceObjects();
        }

        //--------------------------------------------------------------------------------------------------------
        // MULTI-VIEWPORT / PLATFORM INTERFACE SUPPORT
        // This is an _advanced_ and _optional_ feature, allowing the backend to create and handle multiple viewports simultaneously.
        // If you are new to dear imgui or creating a new binding for dear imgui, it is recommended that you completely ignore this section first..
        //--------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper structure we store in the void* RendererUserData field of each ImGuiViewport to easily retrieve our backend data.
        /// </summary>
        private class ViewportData
        {
            public SwapChain SwapChain;
            public RenderTarget RTView;
        };

        private struct ViewportDataHandle
        {
            private nint size;
        }

        private static readonly Dictionary<Pointer<ViewportDataHandle>, ViewportData> viewportData = new();
        private static readonly Silk.NET.SDL.Sdl sdl = Silk.NET.SDL.Sdl.GetApi();

        private static unsafe void CreateWindow(ImGuiViewport* viewport)
        {
            ViewportData vd = new();
            ViewportDataHandle* vh = AllocT<ViewportDataHandle>();
            viewportData.Add(vh, vd);
            viewport->RendererUserData = vh;

            // PlatformHandleRaw should always be a HWND, whereas PlatformHandle might be a higher-level handle (e.g. GLFWWindow*, SDL_Window*).
            // Some backends will leave PlatformHandleRaw == 0, in which case we assume PlatformHandle will contain the HWND.
            Silk.NET.SDL.Window* window = (Silk.NET.SDL.Window*)viewport->PlatformHandle;
            int w, h;
            sdl.GetWindowSize(window, &w, &h);

            SwapChainDescription1 description = new()
            {
                BufferCount = 1,
                Format = Format.R8G8B8A8_UNorm,
                Width = (int)w,
                Height = (int)h,
                SampleDescription = SampleDescription.Default,
                Scaling = Scaling.None,
                Stereo = false,
                SwapEffect = SwapEffect.FlipSequential,
                AlphaMode = AlphaMode.Unspecified,
                Flags = SwapChainFlags.None,
            };

            // Create swap chain
            vd.SwapChain = DXGIDeviceManager.CreateSwapChain(window);

            // Create the render target
            if (vd.SwapChain != null)
            {
                vd.RTView = vd.SwapChain.RenderTarget;
            }
        }

        private static unsafe void DestroyWindow(ImGuiViewport* viewport)
        {
            // The main viewport (owned by the application) will always have RendererUserData == nullptr since we didn't create the data for it.
            ViewportDataHandle* vh = (ViewportDataHandle*)viewport->RendererUserData;
            if (vh != null)
            {
                ViewportData vd = viewportData[vh];
                vd.SwapChain?.Dispose();
                vd.SwapChain = null;
                vd.RTView = null;
                viewportData.Remove(vh);
                Free(vh);
            }
            viewport->RendererUserData = null;
        }

        private static unsafe void SetWindowSize(ImGuiViewport* viewport, Vector2 size)
        {
            ViewportDataHandle* vh = (ViewportDataHandle*)viewport->RendererUserData;
            ViewportData vd = viewportData[vh];

            vd.RTView = null;

            if (vd.SwapChain != null)
            {
                vd.SwapChain.ResizeBuffers(2, (int)size.X, (int)size.Y, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowModeSwitch);
                vd.RTView = vd.SwapChain.RenderTarget;
            }
        }

        private static unsafe void RenderWindow(ImGuiViewport* viewport, void* userdata)
        {
            ViewportDataHandle* vh = (ViewportDataHandle*)viewport->RendererUserData;
            ViewportData vd = viewportData[vh];
            context.OMSetRenderTargets(vd.RTView.RTV, null);
            if ((viewport->Flags & ImGuiViewportFlags.NoRendererClear) != 0)
                context.ClearRenderTargetView(vd.RTView.RTV, new(0.0f, 0.0f, 0.0f, 1.0f));
            RenderDrawData(viewport->DrawData);
        }

        private static unsafe void SwapBuffers(ImGuiViewport* viewport, void* userdata)
        {
            ViewportDataHandle* vh = (ViewportDataHandle*)viewport->RendererUserData;
            ViewportData vd = viewportData[vh];
            vd.SwapChain.Present(0); // Present without vsync
        }

        private static unsafe void InitPlatformInterface()
        {
            ImGuiPlatformIOPtr platform_io = ImGui.GetPlatformIO();
            platform_io.RendererCreateWindow = (void*)Marshal.GetFunctionPointerForDelegate<RendererCreateWindow>(CreateWindow);
            platform_io.RendererDestroyWindow = (void*)Marshal.GetFunctionPointerForDelegate<RendererDestroyWindow>(DestroyWindow);
            platform_io.RendererSetWindowSize = (void*)Marshal.GetFunctionPointerForDelegate<RendererSetWindowSize>(SetWindowSize);
            platform_io.RendererRenderWindow = (void*)Marshal.GetFunctionPointerForDelegate<RendererRenderWindow>(RenderWindow);
            platform_io.RendererSwapBuffers = (void*)Marshal.GetFunctionPointerForDelegate<RendererSwapBuffers>(SwapBuffers);
        }

        private static unsafe void ShutdownPlatformInterface()
        {
            ImGui.DestroyPlatformWindows();
        }
    }
}