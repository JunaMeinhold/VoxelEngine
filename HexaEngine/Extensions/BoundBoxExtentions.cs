using System.Drawing;
using System.Numerics;

namespace HexaEngine.Extensions
{
    public static class BoundBoxExtensions
    {
        public static bool ContainsVector(this RectangleF box, Vector3 vector) => box.Contains(vector.X, vector.Y);

        public static RectangleF BoundingBoxToRect(this RectangleF box) => new(box.X, box.Y, box.Width, box.Height);

        public static RectangleF ToRectNoPos(this RectangleF box) => new(0, 0, box.Width, box.Height);

        public static RectangleF ToRectRMPos(this RectangleF box) => new(0, 0, box.Width, box.Height);
    }
}