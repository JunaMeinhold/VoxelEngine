﻿namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System;
    using System.Numerics;

    public unsafe class SwapChain : IDisposable
    {
        private readonly ComPtr<ID3D11Device> device;
        private readonly ComPtr<IDXGISwapChain1> swapChain;
        private SwapChainDesc1 description;

        private ComPtr<ID3D11Texture2D1> backbuffer;
        private ComPtr<ID3D11RenderTargetView> rtv;

        private readonly DepthStencil depthStencil;
        private bool disposedValue;

        public SwapChain(ComPtr<ID3D11Device> device, ComPtr<IDXGISwapChain1> swapChain, SwapChainDesc1 description)
        {
            this.device = device;
            this.swapChain = swapChain;
            this.description = description;
            InitializeRenderTargets();
            Texture2DDesc desc;
            backbuffer.GetDesc(&desc);
            depthStencil = new((int)desc.Width, (int)desc.Height);
        }

        public ComPtr<ID3D11RenderTargetView> RTV => rtv;

        public ComPtr<ID3D11DepthStencilView> DSV => depthStencil.DSV;

        public DepthStencil DepthStencil => depthStencil;

        private void InitializeRenderTargets()
        {
            swapChain.GetBuffer(0, out backbuffer);
            device.CreateRenderTargetView(backbuffer.As<ID3D11Resource>(), (RenderTargetViewDesc*)null, out rtv);
        }

        public void Present(uint sync)
        {
            swapChain.Present(sync, 0);
        }

        public void ResizeBuffers(int bufferCount, int width, int height, Format format, SwapChainFlag flags)
        {
            rtv.Dispose();
            backbuffer.Dispose();
            swapChain.ResizeBuffers((uint)bufferCount, (uint)width, (uint)height, format, (uint)flags);
            InitializeRenderTargets();
            depthStencil.Resize(width, height);
        }

        public void Resize(int width, int height)
        {
            rtv.Dispose();
            backbuffer.Dispose();
            swapChain.ResizeBuffers(description.BufferCount, (uint)width, (uint)height, description.Format, description.Flags);
            InitializeRenderTargets();
            depthStencil.Resize(width, height);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                depthStencil.Dispose();
                rtv.Dispose();
                backbuffer.Dispose();
                swapChain.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SetTarget(ComPtr<ID3D11DeviceContext> context, bool depth)
        {
            if (depth)
            {
                context.OMSetRenderTargets(1, rtv.GetAddressOf(), depthStencil.DSV);
            }
            else
            {
                context.OMSetRenderTargets(1, rtv.GetAddressOf(), (ID3D11DepthStencilView*)null);
            }
        }

        public void ClearTarget(ComPtr<ID3D11DeviceContext> context, Vector4 color, ClearFlag flag = ClearFlag.Depth | ClearFlag.Stencil, float depth = 1, byte stencil = 0)
        {
            context.ClearRenderTargetView(rtv, (float*)&color);
            context.ClearDepthStencilView(depthStencil, (uint)flag, depth, stencil);
        }
    }
}