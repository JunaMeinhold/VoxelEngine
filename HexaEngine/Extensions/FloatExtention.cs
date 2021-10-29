// <copyright file="FloatExtention.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HexaEngine.Extensions
{
    using System;

    public static class FloatExtention
    {
        public static int CountDivi(this float val0, float val1)
        {
            return (int)Math.Floor(val0 / val1);
        }

        public static bool HasValue(this float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static float MaxToZero(this float value)
        {
            if (value == float.MaxValue | value == float.MinValue)
            {
                return 0;
            }
            else
            {
                return value;
            }
        }

        public static float ExtendValue(this float value)
        {
            var output = value;
            if (value > 0)
            {
                output++;
            }
            else if (value < 0)
            {
                output--;
            }

            return output;
        }

        public static float ExtendValue(this float value, float extender)
        {
            var output = value;
            if (value > 0)
            {
                output += extender;
            }
            else if (value < 0)
            {
                output -= extender;
            }

            return output;
        }

        public static float Invert(this float value) => value * -1;

        public static float Half(this float value) => value / 2;

        public static float ToRadians(this float val)
        {
            return (float)Math.PI / 180 * val;
        }
    }
}