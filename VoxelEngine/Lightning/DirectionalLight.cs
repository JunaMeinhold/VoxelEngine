namespace VoxelEngine.Lightning
{
    using Hexa.NET.Mathematics;
    using VoxelEngine.Mathematics;

    public class DirectionalLight : Light
    {
        public new CameraTransform Transform = new();

        public DirectionalLight()
        {
            base.Transform = Transform;
        }

        public override LightType Type => LightType.Directional;
    }
}