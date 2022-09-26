namespace VoxelEngine.Rendering.D3D.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Field)]
    public class SemanticIndexAttribute : Attribute
    {
        public SemanticIndexAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SemanticOptionalAttribute : Attribute
    {
    }
}