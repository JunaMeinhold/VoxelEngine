namespace HexaEngine.Objects
{
    using HexaEngine.Resources;
    using HexaEngine.Windows;
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;

    public class Crosshair : Disposable
    {
        private readonly Texture texture;
        private ID3D11Buffer vertexBuffer;
        private ID3D11Buffer indexBuffer;
        private VertexBufferView view;

        [StructLayout(LayoutKind.Sequential)]
        public struct OrthoVertex
        {
            public Vector4 position;
            public Vector2 texture;
        }

        public Crosshair(string texturePath)
        {
            texture = ResourceManager.LoadTexture(texturePath);
            DeviceManager.Current.OnResize += Current_OnResize;
            Load();
        }

        public void Render(ID3D11DeviceContext context)
        {
            context.IASetVertexBuffers(0, view);
            context.IASetIndexBuffer(indexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
            context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            texture.Render(context);
        }

        private void Current_OnResize(object sender, EventArgs e)
        {
            Dispose(false);
            Load();
        }

        private void Load()
        {
            int left = -8;
            int top = -8;
            int right = 8;
            int bottom = 8;

            var vertices = new OrthoVertex[]
            {
                new OrthoVertex()
                {
                    position = new Vector4(right, bottom, 0, 1),
                    texture = new Vector2(1, 1)
                },
                     // Top left.
				    new OrthoVertex()
                    {
                        position = new Vector4(left, top, 0, 1),
                        texture = new Vector2(0, 0)
                    },
                    // Bottom right.

                    // Bottom left.
				    new OrthoVertex()
                    {
                        position = new Vector4(left, bottom, 0, 1),
                        texture = new Vector2(0, 1)
                    },
                     new OrthoVertex()
                    {
                        position = new Vector4(right, top, 0, 1),
                        texture = new Vector2(1, 0)
                    },
                    // Top left.
				    new OrthoVertex()
                    {
                        position = new Vector4(left, top, 0, 1),
                        texture = new Vector2(0, 0)
                    },
                     // Top right.

                    // Bottom right.
				    new OrthoVertex()
                    {
                        position = new Vector4(right, bottom, 0, 1),
                        texture = new Vector2(1, 1)
                    }
            };

            var indices = new int[6];

            // Load the index array with data.
            for (var i = 0; i < 6; i++)
                indices[i] = i;

            vertexBuffer = DeviceManager.Current.ID3D11Device.CreateBuffer(vertices, new BufferDescription(Marshal.SizeOf<OrthoVertex>() * 6, BindFlags.VertexBuffer, ResourceUsage.Default));
            indexBuffer = DeviceManager.Current.ID3D11Device.CreateBuffer(indices, new BufferDescription(Marshal.SizeOf<int>() * 6, BindFlags.IndexBuffer, ResourceUsage.Default));
            view = new VertexBufferView(vertexBuffer, Marshal.SizeOf<OrthoVertex>());
        }

        protected override void Dispose(bool disposing)
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            if (disposing)
            {
                texture.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}