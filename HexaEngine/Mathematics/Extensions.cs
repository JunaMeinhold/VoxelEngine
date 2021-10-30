namespace HexaEngine.Mathematics
{
    using System;

    public static class Extensions
    {
        public static int Round(this float x)
        {
            return (int)MathF.Floor(x);
        }
    }
}