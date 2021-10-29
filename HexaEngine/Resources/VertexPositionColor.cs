using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace HexaEngine.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColor
    {
        public Vector4 point;
        public Vector4 color;

        public VertexPositionColor(Vector4 point, Color color)
        {
            this.point = point;
            this.color = new Vector4(color.B, color.G, color.R, color.A);
        }

        public VertexPositionColor(Vector3 point, Color color) : this(new Vector4(point, 1), color)
        {
        }
    }
}