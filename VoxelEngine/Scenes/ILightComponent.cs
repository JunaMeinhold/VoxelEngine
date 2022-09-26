namespace VoxelEngine.Scenes
{
    using VoxelEngine.Rendering.D3D.Interfaces;

    public interface ILightComponent : IComponent
    {
        public IView View { get; }

        public bool CastShadows { get; set; }

        public float ShadowDistance { get; set; }

        public void Update(IView view);
    }

    public interface IDirectionalLightComponent : ILightComponent
    {
    }
}