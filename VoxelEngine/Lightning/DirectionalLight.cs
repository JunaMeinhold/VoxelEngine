namespace VoxelEngine.Lightning
{
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