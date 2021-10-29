// <copyright file="RawVectorExtention.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HexaEngine.Extensions
{
    using HexaEngine;
    using System.Drawing;
    using System.Numerics;

    public static class RawVectorExtention
    {
        public static bool IsSameVector(this Vector2 vector1, Vector2 vector2)
        {
            return vector1.Y == vector2.Y && vector1.X == vector2.X;
        }

        public static PointF ToPoint(this Vector2 vector) => new(vector.X, vector.Y);

        public static SizeF ToSize(this Vector2 vector) => new(vector.X, vector.Y);

        public static Vector2 Downgrade(this Vector3 vec) => new(vec.X, vec.Y);

        public static Vector3 Upgrade(this Vector2 vec) => new(vec.X, vec.Y, 0);

        public static Vector2 SizeToVector(this SizeF size) => new(size.Width, size.Height);

        public static Vector2 Invert(this Vector2 vec) => new(vec.X * -1, vec.Y * -1);

        public static bool IsDefault(this Vector2 vec) => vec.X == 0 && vec.Y == 0;

        public static Vector2 UpgradeToVector2(this float x, float y = 0) => new(x, y);

        public static Vector3 UpgradeToVector3(this Vector2 vec, float z = 0) => new(vec.X, vec.Y, z);

        public static bool InRadius(this Vector2 vec, Vector2 otherVec, float radius)
        {
            return vec.X <= otherVec.X + radius && vec.Y <= otherVec.Y + radius && vec.Y >= otherVec.Y - radius && vec.Y >= otherVec.Y - radius;
        }

        public static Vector3 Invert(this Vector3 vec) => new(vec.X * -1, vec.Y * -1, vec.Z * -1);

        public static Vector3 InvertX(this Vector3 vec) => new(vec.X * -1, vec.Y, vec.Z);

        public static Vector3 InvertY(this Vector3 vec) => new(vec.X, vec.Y * -1, vec.Z);

        public static Vector3 InvertZ(this Vector3 vec) => new(vec.X, vec.Y, vec.Z * -1);

        public static PointF ToPointF(this Vector3 vec) => new(vec.X, vec.Y);
    }
}