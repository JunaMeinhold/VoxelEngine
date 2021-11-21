namespace TestGame
{
    using HexaEngine.Input;
    using HexaEngine.Input.RawInput;
    using HexaEngine.Resources;
    using HexaEngine.Shaders;
    using HexaEngine.Windows;
    using Vortice.Direct3D11;
    using Vortice.Mathematics;

    public class Game : HexaEngine.Game
    {
        public override void Initialize()
        {
            ShaderCache.DisableCache = true;
            Settings = new();
            Settings.InputTypes.Add(HidUsageAndPage.Keyboard);
            Settings.InputTypes.Add(HidUsageAndPage.Mouse);
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

            Scenes.Add(new MainScene());
        }

        public override void InitializeWindow(GameWindow window)
        {
            DeviceManager.Current.SwitchAlpha(true);
            Keyboard.OnKeyUp += (s, e) =>
            {
                if (e.Key == Keys.Escape)
                {
                    window.Close();
                }
                if (e.Key == Keys.F5)
                {
                    window.Scene.Dispatcher.Invoke(() => ResourceManager.ReloadShaders());
                }
                if (e.Key == Keys.F12)
                {
                    Cursor.Visible = !Cursor.Visible;
                    Cursor.Capture = !Cursor.Capture;
                }
            };
            Shader.RequestForReload += (s, e) =>
            {
                window.Scene.Dispatcher.Invoke(() => e.ReloadShader());
            };
        }

        public override void Uninitialize()
        {
        }
    }
}