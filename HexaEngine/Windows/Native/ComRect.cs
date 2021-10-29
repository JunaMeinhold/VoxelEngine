using System.Drawing;
using System.Runtime.InteropServices;

namespace HexaEngine.Windows.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public class ComRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public ComRect()
        {
        }

        public ComRect(Rectangle r)
        {
            Left = r.X;
            Top = r.Y;
            Right = r.Right;
            Bottom = r.Bottom;
        }

        public ComRect(Rect r)
        {
            Left = r.X;
            Top = r.Y;
            Right = r.Right;
            Bottom = r.Bottom;
        }

        public ComRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static implicit operator Rectangle(ComRect r) => Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);

        public static implicit operator ComRect(Rectangle r) => new(r);

        public static implicit operator ComRect(Rect r) => new(r);

        public static ComRect FromXYWH(int x, int y, int width, int height)
        {
            return new ComRect(x, y, x + width, y + height);
        }

        public static bool operator ==(ComRect r1, ComRect r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(ComRect r1, ComRect r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(ComRect r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is ComRect rect)
                return Equals(rect);
            else if (obj is Rectangle rectangle)
                return Equals(new ComRect(rectangle));
            else if (obj is Rect rectangle2)
                return Equals(new ComRect(rectangle2));
            return false;
        }

        public override int GetHashCode()
        {
            return ((Rectangle)this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }
}