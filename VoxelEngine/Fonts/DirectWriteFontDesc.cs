#if D2D1_SUPPORT && DWRITE_SUPPORT

using Vortice.DirectWrite;
using FontStyle = Vortice.DirectWrite.FontStyle;

namespace HexaEngine.Fonts
{
    public struct DirectWriteFontDesc
    {
        public FontStyle FontStyle { get; set; }

        public FontWeight FontWeight { get; set; }

        public float IncrementalTabStop { get; set; }

        public FlowDirection FlowDirection { get; set; }

        public ReadingDirection ReadingDirection { get; set; }

        public WordWrapping WordWrapping { get; set; }

        public FontStretch FontStretch { get; set; }

        public ParagraphAlignment ParagraphAlignment { get; set; }

        public string FontFamilyName { get; set; }

        public TextAlignment TextAlignment { get; set; }

        public float FontSize { get; set; }
    }
}

#endif