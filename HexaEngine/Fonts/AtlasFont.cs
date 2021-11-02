using HexaEngine.IO;
using HexaEngine.Resources;
using HexaEngine.Windows;
using System.Collections.Generic;
using System.Numerics;
using Vortice.Direct3D11;

namespace HexaEngine.Fonts
{
    public class AtlasFont : Font
    {
        public AtlasFont(string path)
        {
            using var fs = FileSystem.Open(ResourceManager.CurrentFontPath + path);
            FontCharacters = new();
            FontCharacters.AddRange(fs.Read<Character>());
            Texture = new();
            Texture.Load(DeviceManager.Current.ID3D11Device, path, fs.Read(fs.Length - fs.Position));
        }

        internal AtlasFont()
        {
        }

        protected override void Dispose(bool disposing)
        {
            Texture?.Dispose();
            Texture = null;
            FontCharacters?.Clear();
            FontCharacters = null;
            base.Dispose(disposing);
        }

        protected override void BuildVertexArray(out List<TextBase.Vertex> vertices, string sentence, float drawX, float drawY)
        {
            // Create list of the vertices
            vertices = new List<TextBase.Vertex>();

            // Draw each letter onto a quad.
            foreach (var ch in sentence)
            {
                char letter = (char)(ch - 32);

                // If the letter is a space then just move over three pixel.
                if (letter == 0)
                    drawX += 3;
                else
                {
                    // Add quad vertices for the character.
                    BuildVertexArray(vertices, letter, ref drawX, ref drawY);

                    // Update the x location for drawing be the size of the letter and one pixel.
                    drawX += FontCharacters[letter].Size + 1;
                }
            }
        }

        protected override void BuildVertexArray(List<TextBase.Vertex> vertices, char letter, ref float drawX, ref float drawY)
        {
            // First triangle in the quad
            vertices.Add // Top left.
            (
                new TextBase.Vertex()
                {
                    Position = new Vector3(drawX, drawY, 0),
                    Texture = new Vector2(FontCharacters[letter].Left, 0)
                }
            );
            vertices.Add // Bottom right.
            (
                new TextBase.Vertex()
                {
                    Position = new Vector3(drawX + FontCharacters[letter].Size, drawY - 16, 0),
                    Texture = new Vector2(FontCharacters[letter].Right, 1)
                }
            );
            vertices.Add // Bottom left.
            (
                new TextBase.Vertex()
                {
                    Position = new Vector3(drawX, drawY - 16, 0),
                    Texture = new Vector2(FontCharacters[letter].Left, 1)
                }
            );
            // Second triangle in quad.
            vertices.Add // Top left.
            (
                new TextBase.Vertex()
                {
                    Position = new Vector3(drawX, drawY, 0),
                    Texture = new Vector2(FontCharacters[letter].Left, 0)
                }
            );
            vertices.Add // Top right.
            (
                new TextBase.Vertex()
                {
                    Position = new Vector3(drawX + FontCharacters[letter].Size, drawY, 0),
                    Texture = new Vector2(FontCharacters[letter].Right, 0)
                }
            );
            vertices.Add // Bottom right.
            (
                new TextBase.Vertex()
                {
                    Position = new Vector3(drawX + FontCharacters[letter].Size, drawY - 16, 0),
                    Texture = new Vector2(FontCharacters[letter].Right, 1)
                }
            );
        }

        public override void Render(ID3D11DeviceContext context)
        {
            context.PSSetShaderResource(0, Texture);
        }
    }
}