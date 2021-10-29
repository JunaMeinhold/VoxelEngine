using HexaEngine.Windows;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Resources
{
    public class RenderPlane : IDisposable
    {     // Structs
        private bool disposedValue;

        [StructLayout(LayoutKind.Sequential)]
        public struct OrthoVertex
        {
            public Vector4 position;
            public Vector2 texture;
        }

        // Properties.
        public ID3D11Buffer VertexBuffer { get; set; }

        public ID3D11Buffer IndexBuffer { get; set; }

        public VertexBufferView VertexBufferView { get; private set; }

        public int VertexCount { get; set; }

        public int IndexCount { get; private set; }

        public DeviceManager Manager { get; }

        public string DebugName { get; }

        // Constructor
        public RenderPlane(DeviceManager deviceManager, string debugName)
        {
            Manager = deviceManager;
            DebugName = debugName;
            Manager.OnResize += Manager_OnBufferResize;
            InitializeBuffers(Manager.ID3D11Device);
        }

        private void Manager_OnBufferResize(object sender, EventArgs e)
        {
            InitializeBuffers(Manager.ID3D11Device);
        }

        public void Render(ID3D11DeviceContext deviceContext)
        {
            deviceContext.IASetVertexBuffers(0, VertexBufferView);
            deviceContext.IASetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
            deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        }

        private void InitializeBuffers(ID3D11Device device)
        {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
            int windowWidth = Manager.Width;
            int windowHeight = Manager.Height;

            float left, right, top, bottom;

            // Calculate the screen coordinates of the left side of the window.
            left = windowWidth / 2 * -1;
            // Calculate the screen coordinates of the right side of the window.
            right = left + windowWidth;
            // Calculate the screen coordinates of the top of the window.
            top = windowHeight / 2;
            // Calculate the screen coordinates of the bottom of the window.
            bottom = top - windowHeight;

            // Set the number of the vertices and indices in the vertex and index array, accordingly.
            VertexCount = 6;
            IndexCount = 6;

            // Create and load the vertex array.
            var vertices = new OrthoVertex[]
            {
                     // Top left.
				    new OrthoVertex()
                    {
                        position = new Vector4(left, top, 0,1),
                        texture = new Vector2(0, 0)
                    },
                    // Bottom right.
				    new OrthoVertex()
                    {
                        position = new Vector4(right, bottom, 0,1),
                        texture = new Vector2(1, 1)
                    },
                    // Bottom left.
				    new OrthoVertex()
                    {
                        position = new Vector4(left, bottom, 0,1),
                        texture = new Vector2(0, 1)
                    },
                    // Top left.
				    new OrthoVertex()
                    {
                        position = new Vector4(left, top, 0,1),
                        texture = new Vector2(0, 0)
                    },
                     // Top right.
				    new OrthoVertex()
                    {
                        position = new Vector4(right, top, 0,1),
                        texture = new Vector2(1, 0)
                    },
                    // Bottom right.
				    new OrthoVertex()
                    {
                        position = new Vector4(right, bottom, 0,1),
                        texture = new Vector2(1, 1)
                    }
            };

            // Create the index array.
            var indices = new int[IndexCount];

            // Load the index array with data.
            for (var i = 0; i < IndexCount; i++)
                indices[i] = i;

            // Set up the description of the static vertex buffer.
            var vertexBuffer = new BufferDescription()
            {
                Usage = ResourceUsage.Default, // ResourceUsage.Dynamic,
                SizeInBytes = Marshal.SizeOf<OrthoVertex>() * VertexCount,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None, // CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            // Create the vertex buffer.
            VertexBuffer = device.CreateBuffer(vertices, vertexBuffer);
            VertexBuffer.DebugName = DebugName + nameof(VertexBuffer);

            // Create the index buffer.
            IndexBuffer = device.CreateBuffer(indices, new BufferDescription(Marshal.SizeOf<int>() * indices.Length, BindFlags.IndexBuffer, ResourceUsage.Default));
            IndexBuffer.DebugName = DebugName + nameof(IndexBuffer);
            VertexBufferView = new VertexBufferView(VertexBuffer, Marshal.SizeOf<OrthoVertex>());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
                IndexBuffer.Dispose();
                IndexBuffer = null;
                disposedValue = true;
            }
        }

        ~RenderPlane()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}