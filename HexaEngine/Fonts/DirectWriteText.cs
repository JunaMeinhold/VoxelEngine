#if D2D1_SUPPORT && DWRITE_SUPPORT

using HexaEngine.Windows;
using System.Numerics;
using Vortice.Direct3D11;

namespace HexaEngine.Fonts
{
    public class DirectWriteText : TextBase
    {
        private readonly ID3D11Texture2D texture;

        public DirectWriteText()
        {
            texture = DeviceManager.Current.ID3D11Device.CreateTexture2D(new Texture2DDescription(Vortice.DXGI.Format.R32G32B32A32_Float, DirectWriteFont.AtlasSize, DirectWriteFont.AtlasSize));
        }

        public override void Render(ID3D11DeviceContext context)
        {
        }

        protected override void UpdateSentece()
        {
            if (Font is DirectWriteFont font)
            {
                font.RenderTo(texture, Color, Vector2.Zero, TextString);
            }
        }
    }
}

#endif