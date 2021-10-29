// <copyright file="ColorExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HexaEngine.Extensions
{
    using System.Drawing;

    public static class ColorExtensions
    {
        public static Color Blend(this Color color, Color backColor)
        {
            int r = (color.R + backColor.R) / 2;
            int g = (color.G + backColor.G) / 2;
            int b = (color.B + backColor.B) / 2;
            int a = (color.A + backColor.A) / 2;
            return Color.FromArgb(a, r, g, b);
        }
    }
}