namespace HexaEngine.Objects.Renderers
{
    using HexaEngine.Resources;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Shaders.BuildIn.Voxel;
    using HexaEngine.Windows;
    using System.Numerics;
    using VoxelGen;

    public class WorldRenderer : IObjectRenderer
    {
        public TextureList TextureList { get; } = new();

        public VoxelShader VoxelShader { get; } = ResourceManager.LoadShader<VoxelShader>();

        public VoxelDepthShader VoxelDepthShader { get; } = ResourceManager.LoadShader<VoxelDepthShader>();

        public bool IsInitialized { get; private set; }

        public void Initialize(DeviceManager manager)
        {
            IsInitialized = true;
            TextureList.AddRange(Registry.Textures);
            TextureList.Load();
        }

        public void Render(DeviceManager manager, IView view, ISceneObject sceneObject, Matrix4x4 transform)
        {
            if (sceneObject is World world)
            {
                foreach (Chunk chunk in world.LoadedChunks)
                {
                    if (view.Frustum.Intersects(chunk.BoundingBox))
                    {
                        TextureList.Render(manager.ID3D11DeviceContext);
                        VoxelShader.Render(view, chunk);
                    }
                }
            }
        }

        public void RenderInstanced(DeviceManager manager, IView view, ISceneObject sceneObject, Matrix4x4 transform, InstanceType[] instances)
        {
            throw new System.NotImplementedException();
        }

        public void Uninitialize()
        {
            IsInitialized = false;
            VoxelShader.Dispose();
            VoxelDepthShader.Dispose();
        }
    }
}