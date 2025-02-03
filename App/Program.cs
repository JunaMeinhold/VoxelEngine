// See https://aka.ms/new-console-template for more information
using App;
using App.Renderers;
using VoxelEngine.Core;
using VoxelEngine.Debugging;
using VoxelEngine.Windows;

Logger.Initialize();
Application.Boot();
Application.Run(new GameWindow(MainScene.Create()) { SceneRenderer = new SceneRenderer() });