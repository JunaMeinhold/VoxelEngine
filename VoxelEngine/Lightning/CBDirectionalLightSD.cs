﻿namespace VoxelEngine.Lightning
{
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public struct CBDirectionalLightSD
    {
        public static readonly unsafe int CascadePointerOffset = sizeof(Matrix4x4) * 8;

        public Matrix4x4 View1;
        public Matrix4x4 View2;
        public Matrix4x4 View3;
        public Matrix4x4 View4;
        public Matrix4x4 View5;
        public Matrix4x4 View6;
        public Matrix4x4 View7;
        public Matrix4x4 View8;
        public float Cascade1;
        public float Cascade2;
        public float Cascade3;
        public float Cascade4;
        public float Cascade5;
        public float Cascade6;
        public float Cascade7;
        public float Cascade8;
        public Vector4 Color;
        public Vector3 Direction;
        public int CastShadows;

        public CBDirectionalLightSD()
        {
            Color = Vector4.Zero;
            Direction = Vector3.Zero;
            CastShadows = default;
        }

        public CBDirectionalLightSD(DirectionalLight light)
        {
            Color = light.Color;
            Direction = light.Transform.Forward;
            CastShadows = light.CastShadows ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Matrix4x4* GetViews(CBDirectionalLightSD* data)
        {
            return (Matrix4x4*)data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float* GetCascades(CBDirectionalLightSD* data)
        {
            return (float*)((byte*)data + CascadePointerOffset);
        }
    }
}