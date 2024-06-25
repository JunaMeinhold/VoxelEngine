namespace VoxelEngine.Rendering.D3D.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Field)]
    public class SemanticNameAttribute : Attribute
    {
        public SemanticNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}