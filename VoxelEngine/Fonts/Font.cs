using HexaEngine.Resources;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System.Collections.Generic;
using Vortice.Direct3D11;

namespace HexaEngine.Fonts
{
    public abstract class Font : Resource
    {
        public List<Character> FontCharacters { get; protected set; }

        public Texture Texture { get; protected set; }

        public void BuildVertexArray(TextBase text, float drawX, float drawY)
        {
            BuildVertexArray(out var vertices, text.TextString, drawX, drawY);
            Shader.SWrite(DeviceManager.Current, text.VertexBuffer, vertices.ToArray());
            vertices?.Clear();
        }

        public abstract void Render(ID3D11DeviceContext context);

        protected abstract void BuildVertexArray(out List<TextBase.Vertex> vertices, string sentence, float drawX, float drawY);

        protected abstract void BuildVertexArray(List<TextBase.Vertex> vertices, char letter, ref float drawX, ref float drawY);
    }
}