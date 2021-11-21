namespace HexaEngine
{
    using HexaEngine.Windows;
    using System.Collections.Generic;

    public abstract class Game
    {
        public abstract void Initialize();

        public abstract void InitializeWindow(GameWindow window);

        public abstract void Uninitialize();

        public List<ScenePrefab> Scenes { get; set; } = new();

        public GameSettings Settings { get; set; }
    }
}