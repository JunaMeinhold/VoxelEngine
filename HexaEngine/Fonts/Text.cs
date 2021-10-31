using HexaEngine.Windows;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;

namespace HexaEngine.Fonts
{
    public class Text
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex
        {
            public Vector3 Position;
            public Vector2 Texture;
        }

        public Font Font;
        public ID3D11Buffer VertexBuffer;
        public ID3D11Buffer IndexBuffer;
        private int VertexCount;
        public int IndexCount;
        private int MaxLength;
        private Vector2 position;
        private string textString;

        public DeviceManager Manager { get; }

        public string TextString { get => textString; set { textString = value; UpdateSentece(); } }

        public Vector4 Color { get; set; } = new Vector4(1, 1, 1, 1);

        public Vector2 Position { get => position; set { position = value; UpdateSentece(); } }

        public Matrix4x4 Transform { get; set; }

        public Text(DeviceManager manager, Font font, string text, int maxLength = int.MaxValue)
        {
            Manager = manager;
            Font = font;

            // Initialize the sentence buffers to null;
            VertexBuffer = null;
            IndexBuffer = null;

            // Set the maximum length of the sentence.
            MaxLength = maxLength;

            // Set the number of vertices in vertex array.
            VertexCount = 6 * maxLength;

            // Set the number of vertices in the vertex array.
            IndexCount = VertexCount;

            // Create the vertex array.
            var vertices = new Vertex[VertexCount];
            // Create the index array.
            var indices = new int[IndexCount];

            // Initialize the index array.
            for (var i = 0; i < IndexCount; i++)
                indices[i] = i;

            // Set up the description of the dynamic vertex buffer.
            var vertexBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = Marshal.SizeOf<Vertex>() * VertexCount,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            // Create the vertex buffer.
            VertexBuffer = manager.ID3D11Device.CreateBuffer(vertices, vertexBufferDesc);

            // Create the index buffer.
            IndexBuffer = manager.ID3D11Device.CreateBuffer(BindFlags.IndexBuffer, indices);

            TextString = text;
        }

        protected void UpdateSentece()
        {
            // Get the number of the letter in the sentence.
            var numLetters = TextString.Length;

            // Check for possible buffer overflow.
            if (numLetters > MaxLength)
                return;

            // Calculate the X and Y pixel position on screen to start drawing to.
            var drawX = Position.X;
            var drawY = Position.Y;

            // Use the font class to build the vertex array from the sentence text and sentence draw location.
            Font.BuildVertexArray(this, drawX, drawY);
        }

        public void Dispose()
        {
            // Release the sentence vertex buffer.
            VertexBuffer?.Dispose();
            VertexBuffer = null;
            // Release the sentence index buffer.
            IndexBuffer?.Dispose();
            IndexBuffer = null;
        }

        public void Render(ID3D11DeviceContext context)
        {
            Font.Render(context);
            context.IASetVertexBuffers(0, new VertexBufferView(VertexBuffer, Marshal.SizeOf<Vertex>(), 0));
            context.IASetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
            context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        }
    }
}