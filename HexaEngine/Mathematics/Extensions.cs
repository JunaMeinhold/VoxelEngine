namespace HexaEngine.Mathematics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class Extensions
    {
        public static int Round(this float x)
        {
            return (int)MathF.Floor(x);
        }
    }
}