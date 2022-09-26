namespace VoxelEngine.Rendering.D3D.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Field)]
    public class OffsetAttribute : Attribute
    {
        public OffsetAttribute(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; set; }
    }
}