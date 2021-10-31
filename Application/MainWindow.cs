using HexaEngine.Fonts;
using HexaEngine.Input.Events;
using HexaEngine.Input.RawInput;
using HexaEngine.Objects;
using HexaEngine.Objects.Renderers;
using HexaEngine.Resources;
using HexaEngine.Scenes;
using HexaEngine.Scenes.Objects;
using HexaEngine.Windows;
using System.Numerics;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace App
{
    public class MainWindow : RenderWindow
    {
        public MainWindow() : base("Test", 1280, 720)
        {
            VSync = true;
            Fullscreen = false;
            StartupLocation = StartupLocation.Center;
        }

        protected override void InitializeComponent()
        {
            //SetBorderlessFullscreen(true);
            RegisterControl(HidUsageAndPage.Mouse);
            RegisterControl(HidUsageAndPage.Keyboard);
            Cursor.Capture = true;
            Cursor.Visible = false;
            FPSLimit = false;
            BackgroundClear = System.Drawing.Color.Black;

            Registry.RegisterBlock("dirt.png");
            Registry.RegisterBlock("stone.png");
            Registry.RegisterBlock("grass_top.png", "grass_side.png", "dirt.png");

            Texture.DefaultSamplerDescription = new SamplerDescription()
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Clamp,
                MipLODBias = 0,
                MaxAnisotropy = 1,
                ComparisonFunction = ComparisonFunction.Always,
                BorderColor = new Color4(0, 0, 0, 0),
                MinLOD = 0,
                MaxLOD = float.MaxValue
            };
            Scene.Renderers.Add(new DeferredRenderer());
            var ui = new UIRenderer() { Crosshair = new Crosshair("crosshair.png") };
            var font = new Font("font0.ff");
            var str = "Pre-Alpha 0.11";
            var text = new Text(DeviceManager, font, str, str.Length) { Transform = Matrix4x4.CreateTranslation(-(1280 / 2), 720 / 2, 0) };
            ui.Texts.Add(text);
            var world = new World("world/");
            var player = new Player(world);
            player.Camera.Listener = DeviceManager.AudioManager.Listener;
            Scene.Camera = player.Camera;
            Scene.Objects.Add(player);
            world.Player = player;
            world.RenderDistance = 16;
            world.Generator = new PerlinChunkGenerator(1332);
            Scene.Objects.Add(world);
            Scene.UIRenderers.Add(ui);
            Scene.ForwardRenderers.Add(new SkyboxRenderer() { Skybox = new Skybox("skybox.obj", "sky_box.dds") });
            DeviceManager.SwitchAlpha(true);
        }

        protected override void OnKeyUp(KeyboardEventArgs keyboardEventArgs)
        {
            base.OnKeyUp(keyboardEventArgs);
            if (keyboardEventArgs.Key == HexaEngine.Input.Keys.Escape)
            {
                Close();
            }
        }
    }
}