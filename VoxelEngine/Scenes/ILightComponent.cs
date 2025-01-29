namespace VoxelEngine.Scenes
{
    using VoxelEngine.Graphics.D3D11.Interfaces;

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