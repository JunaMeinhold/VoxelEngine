namespace App
{
    using App.Objects;
    using App.Renderers;
    using App.Renderers.Forward;
    using App.Scripts;
    using VoxelEngine.Scenes;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;
    using VoxelEngine.Voxel.WorldGen;

    public class MainScene : Scene
    {
        public override void Initialize()
        {
            // Render callbacks for the scene
            Renderer = new MainSceneDeferredRenderer();
            Camera = new Camera();
            Camera.Far = 1000;
            Add(Camera);
            // Creates the skybox.
            Add(new Skybox());

            // Creates the crosshair.
            Add(new Crosshair());

            // Creates the world.
            World world = new("world");
            world.Generator = new PerlinChunkGenerator(68458);
            world.AddComponent(new WorldController());
            world.AddComponent(new BlockHighlightRenderer());
            Add(world);

            // Creates the player.
            CPlayer player = new(new(0, 16, 0));
            Add(player);

            // Registers the block types.
            BlockRegistry.Reset();
            BlockRegistry.RegisterBlock(new("Dirt", new("blocks/dirt.dds")));
            BlockRegistry.RegisterBlock(new("Stone", new("blocks/stone.dds")));
            BlockRegistry.RegisterBlock(new("Grass", new("blocks/grass_top.dds", "blocks/dirt.dds", "blocks/grass_side.dds")));
            BlockRegistry.RegisterBlock(new("Stonebrick", new("blocks/stonebrick.dds")));
            BlockRegistry.RegisterBlock(new("Cobblestone", new("blocks/cobblestone.dds")));
            BlockRegistry.RegisterBlock(new("Brick", new("blocks/brick.dds")));
            BlockRegistry.RegisterBlock(new("Iron Block", new("blocks/iron_block.dds")));
            BlockRegistry.RegisterBlock(new("Oak Log", new("blocks/log_oak_top.dds", "blocks/log_oak_top.dds", "blocks/log_oak.dds")));
            BlockRegistry.RegisterBlock(new("Oak Planks", new("blocks/planks_oak.dds")));
            BlockRegistry.RegisterBlock(new("Quartz Block", new("blocks/quartz_block.dds")));

            base.Initialize();
        }
    }
}