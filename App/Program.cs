// See https://aka.ms/new-console-template for more information
using App;
using VoxelEngine.Core;
using VoxelEngine.Debugging;
using VoxelEngine.Windows;

int value = 134483968;

int v = ((value >> 18) & (31));

Logger.Initialize();
Application.Boot();
Application.Run(new GameWindow(MainScene.Create()));