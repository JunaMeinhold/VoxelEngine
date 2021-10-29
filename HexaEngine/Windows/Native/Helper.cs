using System;
using System.Drawing;

namespace HexaEngine.Windows.Native
{
    public static class Helper
    {
        public static int SignedLOWORD(int n)
        {
            return (short)(n & 0xFFFF);
        }

        public static int SignedHIWORD(int n)
        {
            return (n >> 16) & 0xFFFF;
        }

        public static int SignedLOWORD(IntPtr intPtr)
        {
            return SignedLOWORD(IntPtrToInt32(intPtr));
        }

        public static int SignedHIWORD(IntPtr intPtr)
        {
            return SignedHIWORD(IntPtrToInt32(intPtr));
        }

        public static int IntPtrToInt32(IntPtr intPtr)
        {
            return (int)intPtr.ToInt64();
        }

        public static Point MakePoint(IntPtr lparam)
        {
            var lp = lparam.ToInt64();
            var x = lp & 0xffff;
            var y = (lp >> 16) & 0xffff;
            return new Point((int)x, (int)y);
        }
    }
}