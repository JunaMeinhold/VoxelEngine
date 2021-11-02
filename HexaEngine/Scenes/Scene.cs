namespace HexaEngine.Scenes
{
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Scenes.Objects;
    using HexaEngine.Windows;
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public class Scene
    {
        public Scene(RenderWindow window, DeviceManager deviceManager)
        {
            Window = window;
            DeviceManager = deviceManager;
        }

        public List<ISceneObject> Objects { get; } = new();

        public List<IRenderer> Renderers { get; } = new();

        public List<IForwardRenderer> ForwardRenderers { get; } = new();

        public List<IForwardRenderer> UIRenderers { get; } = new();

        public List<IObjectRenderer> ObjectRenderers { get; } = new();

        public RenderWindow Window { get; }

        public DeviceManager DeviceManager { get; }

        public SceneDispatcher Dispatcher { get; } = new();

        public Camera Camera { get; set; }

        public void Initialize()
        {
            Renderers.ForEach(x => { if (!x.IsInitialized) x.Initialize(DeviceManager); });
            ForwardRenderers.ForEach(x => { if (!x.IsInitialized) x.Initialize(DeviceManager); });
            UIRenderers.ForEach(x => { if (!x.IsInitialized) x.Initialize(DeviceManager); });
            ForEach<ISceneObject>(x => { if (!x.Renderer?.IsInitialized ?? false) x.Renderer.Initialize(DeviceManager); });
            ForEach<IScriptObject>(x => x.Initialize());
            Time.FixedUpdate += FixedUpdate;
            _ = Time.Initialize();
        }

        public void Render()
        {
            Time.FrameUpdate();
            Camera.UpdateView();
            Simulate(Time.Delta);

            Dispatcher.ExecuteInvokes();

            Renderers.ForEach(x => x.BeginRender(DeviceManager));
            Objects.ForEach(x => x.Renderer?.Render(DeviceManager, Camera, x, Matrix4x4.Identity));
            DeviceManager.ClearRenderTarget();
            DeviceManager.SetRenderTarget();
            ForwardRenderers.ForEach(x => x.Render(DeviceManager, Camera));
            Renderers.ForEach(x => x.EndRender(DeviceManager, Camera));
            UIRenderers.ForEach(x => x.Render(DeviceManager, Camera));
        }

        public void ForEach<T>(Action<T> action) where T : class, ISceneObject
        {
            foreach (var sceneObject in Objects)
            {
                if (sceneObject is T t)
                {
                    action.Invoke(t);
                }
            }
        }

        private void FixedUpdate(object sender, EventArgs e)
        {
            ForEach<IScriptObject>(x => x.UpdateFixed());
        }

        public void Simulate(float delta)
        {
            ForEach<IFrameScriptObject>(x => x.Update());
        }

        public void Unload()
        {
            ForEach<IScriptObject>(x => x.Uninitialize());
            ForEach<ISceneObject>(x => { if (x.Renderer?.IsInitialized ?? false) x.Renderer.Uninitialize(); });
            Renderers.ForEach(x => { if (x.IsInitialized) x.Uninitialize(); });
            ForwardRenderers.ForEach(x => { if (x.IsInitialized) x.Uninitialize(); });
            UIRenderers.ForEach(x => { if (x.IsInitialized) x.Uninitialize(); });
        }
    }
}