namespace VoxelEngine.Rendering.D3D.Attributes
{
    using System;
    using Vortice.DXGI;

    [AttributeUsage(AttributeTargets.Field)]
    public class FormatAttribute : Attribute
    {
        public FormatAttribute(Format format)
        {
            Format = format;
        }

        public Format Format { get; set; }
    }
}