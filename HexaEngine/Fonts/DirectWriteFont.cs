#if D2D1_SUPPORT && DWRITE_SUPPORT

using HexaEngine.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;

namespace HexaEngine.Fonts
{
    public class DirectWriteFont : Font
    {
        public const int AtlasSize = 4096;

        private DirectWriteFont()
        {
        }

        public DirectWriteFont(string fontFamily, int fontSize)
        {
            TextFormat = DeviceManager.Current.IDWriteFactory.CreateTextFormat(fontFamily, fontSize);
        }

        public DirectWriteFont(DirectWriteFontDesc desc)
        {
            TextFormat = DeviceManager.Current.IDWriteFactory.CreateTextFormat(desc.FontFamilyName, desc.FontWeight, desc.FontStyle, desc.FontStretch, desc.FontSize);
            TextFormat.IncrementalTabStop = desc.IncrementalTabStop;
            TextFormat.FlowDirection = desc.FlowDirection;
            TextFormat.ReadingDirection = desc.ReadingDirection;
            TextFormat.WordWrapping = desc.WordWrapping;
            TextFormat.ParagraphAlignment = desc.ParagraphAlignment;
            TextFormat.TextAlignment = desc.TextAlignment;
        }

        public IDWriteTextFormat TextFormat { get; }

        public IDWriteTextLayout CreateLayout(string str, float maxWidth, float maxHeight)
        {
            return DeviceManager.Current.IDWriteFactory.CreateTextLayout(str, TextFormat, maxWidth, maxHeight);
        }

        public static Vector2 GetSize(IDWriteTextLayout layout)
        {
            return new Vector2(GetWidth(layout), layout?.Metrics.Height ?? 0);
        }

        public static float GetWidth(IDWriteTextLayout layout)
        {
            float width = layout?.Metrics.WidthIncludingTrailingWhitespace ?? 0;
            return width == 0 ? 2 : width;
        }

        public IDWriteTextLayout RenderTo(ID3D11Texture2D texture, Vector4 color, Vector2 origin, string text)
        {
            var layout = CreateLayout(text, AtlasSize, AtlasSize);
            RenderTo(texture, color, origin, layout);
            return layout;
        }

        public static void RenderTo(ID3D11Texture2D texture, Vector4 color, Vector2 origin, IDWriteTextLayout layout)
        {
            var surface = texture.QueryInterface<IDXGISurface>();
            var target = DeviceManager.Current.ID2D1Factory.CreateDxgiSurfaceRenderTarget(surface,
                new RenderTargetProperties(
                    RenderTargetType.Default,
                    new Vortice.DCommon.PixelFormat(Format.Unknown, Vortice.DCommon.AlphaMode.Premultiplied),
                    96,
                    96,
                    RenderTargetUsage.None,
                    FeatureLevel.Default
                ));
            target.BeginDraw();
            var brush = target.CreateSolidColorBrush(Color.FromArgb((int)color.W, (int)color.X, (int)color.Y, (int)color.Z));
            target.DrawTextLayout(new(origin.X, origin.Y), layout, brush);
            _ = target.EndDraw();
            brush.Dispose();
            target.Dispose();
            surface.Dispose();
        }

        public void BuildVertexArray(DirectWriteText text, IDWriteTextLayout layout)
        {
        }

        protected override void BuildVertexArray(out List<TextBase.Vertex> vertices, string sentence, float drawX, float drawY)
        {
            throw new System.NotImplementedException();
        }

        protected override void BuildVertexArray(List<TextBase.Vertex> vertices, char letter, ref float drawX, ref float drawY)
        {
            throw new System.NotImplementedException();
        }

        public override void Render(ID3D11DeviceContext context)
        {
            throw new System.NotImplementedException("Only use Text Render");
        }
    }
}

#endif