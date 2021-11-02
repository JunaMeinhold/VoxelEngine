using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace HexaEngine.Fonts
{
    public abstract class TextBase
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex
        {
            public Vector3 Position;
            public Vector2 Texture;
        }

        private string textString;

        public string TextString { get => textString; set { textString = value; UpdateSentece(); } }

        public ID3D11Buffer VertexBuffer { get; set; }

        public ID3D11Buffer IndexBuffer { get; set; }

        public Matrix4x4 Transform { get; set; }

        public Font Font { get; set; }

        public Vector4 Color { get; set; } = new Vector4(1, 1, 1, 1);

        public int VertexCount { get; set; }

        public int IndexCount { get; set; }

        public abstract void Render(ID3D11DeviceContext context);

        protected abstract void UpdateSentece();
    }
}