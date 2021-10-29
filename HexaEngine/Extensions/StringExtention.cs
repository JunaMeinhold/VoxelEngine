// <copyright file="StringExtention.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HexaEngine.Extensions
{
    using System.Globalization;
    using System.IO;

    public static class StringExtention
    {
        public static int ToInt(this string str) => int.Parse(str, NumberStyles.Any, CultureInfo.CurrentCulture);

        public static float ToFloat(this string str) => float.Parse(str, NumberStyles.Any, CultureInfo.CurrentCulture);

        public static string ToString(this string[] strings, string separator) => string.Join(separator, strings);

        public static string GetAbsPath(this string path) => Path.GetFullPath(path);
    }
}