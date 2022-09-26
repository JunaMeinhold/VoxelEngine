namespace VoxelEngine.Lightning
{
    using System.Numerics;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CBDirectionalLightSD
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public Matrix4x4[] Views;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] Cascades;

        public Vector4 Color;
        public Vector3 Direction;
        public int padd;

        public CBDirectionalLightSD()
        {
            Views = new Matrix4x4[16];
            Cascades = new float[16];
            Color = Vector4.Zero;
            Direction = Vector3.Zero;
            padd = default;
        }

        public CBDirectionalLightSD(DirectionalLight light)
        {
            Views = new Matrix4x4[16];
            Cascades = new float[16];
            Color = light.Color;
            Direction = light.Transform.Forward;
            padd = default;
        }
    }
}