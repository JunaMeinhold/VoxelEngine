using HexaEngine.Windows;
using TestGame;

namespace App
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Game game = new();
            game.Initialize();
            Application.Run(new GameWindow(game));
            game.Uninitialize();
        }
    }
}