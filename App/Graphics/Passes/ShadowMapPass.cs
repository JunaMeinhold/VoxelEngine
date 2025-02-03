using App.Renderers;

namespace App.Graphics.Passes
{
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using Hexa.NET.DXGI;
    using HexaEngine.Graphics.Effects.Blur;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lightning;
    using VoxelEngine.Scenes;

    public class ShadowMapPass : RenderPass
    {
        private ConstantBuffer<CSMBuffer> csmBuffer = null!;
        private GaussianBlur blurFilter = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            blurFilter = new(Format.R32G32Float, Nucleus.Settings.ShadowMapSize, Nucleus.Settings.ShadowMapSize);
            csmBuffer = creator.CreateConstantBuffer<CSMBuffer>("CSMBuffer", CpuAccessFlags.Write).Value!;
            D3D11GlobalResourceList.SetCBV("CSMCascadeBuffer", csmBuffer);
        }

        protected override void DisposeCore()
        {
            D3D11GlobalResourceList.SetCBV("CSMCascadeBuffer", null);
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            var directionalLight = scene.LightSystem.ActiveDirectionalLight;
            if (directionalLight == null)
            {
                return;
            }

            directionalLight.Update(context, csmBuffer);

            SkyboxRenderer.SunDir = directionalLight.Transform.Forward;

            directionalLight.PrepareDraw(context);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);
            scene.RenderSystem.Draw(context, RenderQueueIndex.GeometryLast, PassIdentifer.DirectionalLightShadowPass, camera, directionalLight);

            FilterArray(context, directionalLight.ShadowMap!);

            context.ClearState();

            D3D11GlobalResourceList.SetSRV("CSMDepthBuffer", directionalLight.ShadowMap);
        }

        private void FilterArray(GraphicsContext context, Texture2D source)
        {
            if (source.Width != blurFilter.Width || source.Height != blurFilter.Height || source.Format != blurFilter.Format)
            {
                blurFilter.Resize(source.Format, source.Width, source.Height);
            }

            for (int i = 0; i < source.ArraySize; i++)
            {
                blurFilter.Blur(context, source.SRVArraySlices![i], source.RTVArraySlices![i], source.Width, source.Height);
            }
        }
    }
}