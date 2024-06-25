namespace VoxelEngine.Core
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Silk.NET.SDL;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Core.Input.Events;
    using VoxelEngine.Core.Windows;
    using VoxelEngine.Core.Windows.Events;

    public static unsafe class Application
    {
        public static readonly Sdl sdl = Sdl.GetApi();

        private static bool initialized = false;
        private static bool exiting = false;

        private static readonly Dictionary<uint, SdlWindow> windowIdToWindow = new();
        private static readonly List<SdlWindow> windows = new();
        private static readonly List<Func<Event, bool>> hooks = new();
        private static SdlWindow mainWindow;

#nullable disable
        public static SdlWindow MainWindow => mainWindow;
#nullable enable

        public enum SpecialFolder
        {
            Assets,
            Shaders,
            Sounds,
            Models,
            Textures,
            Scenes,
        }

        public static string GetFolder(SpecialFolder folder)
        {
            return folder switch
            {
                SpecialFolder.Assets => Path.GetFullPath("assets/"),
                SpecialFolder.Shaders => Path.GetFullPath("assets/shaders/"),
                SpecialFolder.Sounds => Path.GetFullPath("assets/sounds/"),
                SpecialFolder.Models => Path.GetFullPath("assets/models/"),
                SpecialFolder.Textures => Path.GetFullPath("assets/textures/"),
                SpecialFolder.Scenes => Path.GetFullPath("assets/scenes/"),
                _ => throw new ArgumentOutOfRangeException(nameof(folder)),
            };
        }

        public static void Boot()
        {
            sdl.SetHint(Sdl.HintMouseFocusClickthrough, "1");
            sdl.SetHint(Sdl.HintAutoUpdateJoysticks, "1");
            sdl.SetHint(Sdl.HintJoystickHidapiPS4, "1");
            sdl.SetHint(Sdl.HintJoystickHidapiPS4Rumble, "1");
            sdl.SetHint(Sdl.HintJoystickRawinput, "0");
            sdl.Init(Sdl.InitEvents + Sdl.InitGamecontroller + Sdl.InitHaptic + Sdl.InitJoystick + Sdl.InitSensor);

            SdlCheckError();

            Keyboard.Init();
            SdlCheckError();
            Mouse.Init();
            SdlCheckError();
            Gamepads.Init();
            SdlCheckError();
            TouchDevices.Init();
            SdlCheckError();
        }

        public static void Run(SdlWindow mainWindow)
        {
            Application.mainWindow = mainWindow;

            mainWindow.Show();
            Init();
            mainWindow.Closing += MainWindowClosing;

            PlatformRun();
        }

        /// <summary>
        /// Registers a window to the application.
        /// </summary>
        /// <param name="window">The window to register.</param>
        internal static void RegisterWindow(SdlWindow window)
        {
            windows.Add(window);
            windowIdToWindow.Add(window.WindowID, window);
            if (initialized)
            {
                window.RendererCreate();
            }
        }

        private static void MainWindowClosing(object? sender, CloseEventArgs e)
        {
            if (!e.Handled)
            {
                exiting = true;
            }
        }

        public static void RegisterHook(Func<Event, bool> hook)
        {
            hooks.Add(hook);
        }

        public static void UnregisterHook(Func<Event, bool> hook)
        {
            hooks.Remove(hook);
        }

        private static void Init()
        {
            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].RendererCreate();
            }

            initialized = true;
        }

        private static void PlatformRun()
        {
            Event evnt;
            Time.Initialize();

            while (!exiting)
            {
                sdl.PumpEvents();
                while (sdl.PollEvent(&evnt) == (int)SdlBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        hooks[i](evnt);
                    }
                    EventType type = (EventType)evnt.Type;
                    switch (type)
                    {
                        case EventType.Firstevent:
                            break;

                        case EventType.Quit:
                            exiting = true;
                            break;

                        case EventType.AppTerminating:
                            exiting = true;
                            break;

                        case EventType.AppLowmemory:
                            break;

                        case EventType.AppWillenterbackground:
                            break;

                        case EventType.AppDidenterbackground:
                            break;

                        case EventType.AppWillenterforeground:
                            break;

                        case EventType.AppDidenterforeground:
                            break;

                        case EventType.Localechanged:
                            break;

                        case EventType.Displayevent:
                            break;

                        case EventType.Windowevent:
                            {
                                var even = evnt.Window;
                                if (even.WindowID == mainWindow.WindowID)
                                {
                                    mainWindow.ProcessEvent(even);
                                    if ((WindowEventID)evnt.Window.Event == WindowEventID.Close)
                                    {
                                        exiting = true;
                                    }
                                }
                            }

                            break;

                        case EventType.Syswmevent:
                            break;

                        case EventType.Keydown:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputKeyboard(even);
                            }
                            break;

                        case EventType.Keyup:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputKeyboard(even);
                            }
                            break;

                        case EventType.Textediting:
                            break;

                        case EventType.Textinput:
                            {
                                var even = evnt.Text;
                                Keyboard.OnTextInput(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputText(even);
                            }
                            break;

                        case EventType.Keymapchanged:
                            break;

                        case EventType.Mousemotion:
                            {
                                var even = evnt.Motion;
                                Mouse.OnMotion(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Mousebuttondown:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Mousebuttonup:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Mousewheel:
                            {
                                var even = evnt.Wheel;
                                Mouse.OnWheel(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    mainWindow.ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Joyaxismotion:
                            {
                                var even = evnt.Jaxis;
                                Joysticks.OnAxisMotion(even);
                            }
                            break;

                        case EventType.Joyballmotion:
                            {
                                var even = evnt.Jball;
                                Joysticks.OnBallMotion(even);
                            }
                            break;

                        case EventType.Joyhatmotion:
                            {
                                var even = evnt.Jhat;
                                Joysticks.OnHatMotion(even);
                            }
                            break;

                        case EventType.Joybuttondown:
                            {
                                var even = evnt.Jbutton;
                                Joysticks.OnButtonDown(even);
                            }
                            break;

                        case EventType.Joybuttonup:
                            {
                                var even = evnt.Jbutton;
                                Joysticks.OnButtonUp(even);
                            }
                            break;

                        case EventType.Joydeviceadded:
                            {
                                var even = evnt.Jdevice;
                                Joysticks.AddJoystick(even);
                            }
                            break;

                        case EventType.Joydeviceremoved:
                            {
                                var even = evnt.Jdevice;
                                Joysticks.RemoveJoystick(even);
                            }
                            break;

                        case EventType.Controlleraxismotion:
                            {
                                var even = evnt.Caxis;
                                Gamepads.AxisMotion(even);
                            }
                            break;

                        case EventType.Controllerbuttondown:
                            {
                                var even = evnt.Cbutton;
                                Gamepads.ButtonDown(even);
                            }
                            break;

                        case EventType.Controllerbuttonup:
                            {
                                var even = evnt.Cbutton;
                                Gamepads.ButtonUp(even);
                            }
                            break;

                        case EventType.Controllerdeviceadded:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.AddController(even);
                            }
                            break;

                        case EventType.Controllerdeviceremoved:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.RemoveController(even);
                            }
                            break;

                        case EventType.Controllerdeviceremapped:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.Remapped(even);
                            }
                            break;

                        case EventType.Controllertouchpaddown:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.TouchPadDown(even);
                            }
                            break;

                        case EventType.Controllertouchpadmotion:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.TouchPadMotion(even);
                            }
                            break;

                        case EventType.Controllertouchpadup:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.TouchPadUp(even);
                            }
                            break;

                        case EventType.Controllersensorupdate:
                            {
                                var even = evnt.Csensor;
                                Gamepads.SensorUpdate(even);
                            }
                            break;

                        case EventType.Fingerdown:
                            break;

                        case EventType.Fingerup:
                            break;

                        case EventType.Fingermotion:
                            break;

                        case EventType.Dollargesture:
                            break;

                        case EventType.Dollarrecord:
                            break;

                        case EventType.Multigesture:
                            break;

                        case EventType.Clipboardupdate:
                            break;

                        case EventType.Dropfile:
                            break;

                        case EventType.Droptext:
                            break;

                        case EventType.Dropbegin:
                            break;

                        case EventType.Dropcomplete:
                            break;

                        case EventType.Audiodeviceadded:
                            break;

                        case EventType.Audiodeviceremoved:
                            break;

                        case EventType.Sensorupdate:
                            break;

                        case EventType.RenderTargetsReset:
                            break;

                        case EventType.RenderDeviceReset:
                            break;

                        case EventType.Userevent:
                            break;

                        case EventType.Lastevent:
                            break;
                    }
                }

                mainWindow.Render();
                mainWindow.ClearState();
                Time.FrameUpdate();
            }

            mainWindow.RendererDestroy();

            sdl.Quit();
        }
    }
}