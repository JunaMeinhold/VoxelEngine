namespace HexaEngine.Windows
{
    using System.Linq;

    public class GameWindow : RenderWindow
    {
        public GameWindow(Game game) : base(game.Settings.Title, game.Settings.Width, game.Settings.Height)
        {
            Game = game;
            Settings = Game.Settings;
            VSync = Settings.VSync;
            Fullscreen = Settings.Fullscreen;
            StartupLocation = Settings.StartupLocation;
            FPSLimit = Settings.FPSLimit;
            FPSTarget = Settings.FPSTarget;
            BackgroundClear = Settings.BackgroundClear;
        }

        public Game Game { get; }

        public GameSettings Settings { get; }

        protected override void InitializeComponent()
        {
            Cursor.Capture = Settings.CursorCapture;
            Cursor.Visible = Settings.CursorVisible;
            foreach (var item in Settings.InputTypes)
            {
                RegisterControl(item);
            }

            Scene = Game.Scenes.First().InternalCreateInstance(this);
            Game.InitializeWindow(this);
        }
    }
}