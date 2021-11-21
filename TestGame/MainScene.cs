namespace TestGame
{
    using BepuPhysics;
    using BepuPhysics.Collidables;
    using HexaEngine;
    using HexaEngine.Scenes.Objects;
    using HexaEngine.Scenes.Renderers;
    using HexaEngine.Scripting;
    using HexaEngine.Shaders.BuildIn.Deferred;
    using System.Numerics;

    public class MainScene : ScenePrefab
    {
        public override void CreateInstance()
        {
            Scene.Camera = new()
            {
                Fov = 120f,
                Type = HexaEngine.Scenes.CameraType.Perspective,
                PositionZ = -5,
                PositionY = 2
            };

            CameraController cameraController = new(Scene.Camera);
            Scene.Elements.Add(cameraController);

            HexaElement element = new();
            var sphere = new Sphere(1);
            sphere.ComputeInertia(1, out var sphereInertia);
            element.AddComponent(new RendererComponent() { Model = "Sphere.obj", Texture = "dirt.png", Shader = typeof(DeferredShader) });
            element.AddComponent(new PhysicsBodyComponent() { BodyDescription = BodyDescription.CreateDynamic(new Vector3(0, 5, 0), sphereInertia, new CollidableDescription(Scene.Simulation.Shapes.Add(sphere), 0f), new BodyActivityDescription(0.01f)) });
            Scene.Elements.Add(element);

            HexaElement element1 = new();
            element1.AddComponent(new RendererComponent() { Model = "Box.obj", Texture = "stone.png", Shader = typeof(DeferredShader) });
            element1.AddComponent(new PhysicsStaticComponent() { StaticDescription = new StaticDescription(new Vector3(0, 0, 0), new CollidableDescription(Scene.Simulation.Shapes.Add(new BepuPhysics.Collidables.Box(1000, 2, 1000)), 0.0f)) });
            Scene.Elements.Add(element1);
            Scene.DeferredRenderer = new DeferredRenderer();
            Scene.ForwardRenderers.Add(new SkyboxRenderer() { Skybox = new Skybox("skybox.obj", "sky_box.dds") });
        }
    }
}