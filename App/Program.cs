// See https://aka.ms/new-console-template for more information
using App;
using VoxelEngine.Core;
using VoxelEngine.Windows;

#if DEBUG
Nucleus.Settings.ShaderCache = false;
#else
Nucleus.Settings.ShaderCache = true;
#endif
Application.Boot();
Application.Run(new GameWindow(new MainScene()));