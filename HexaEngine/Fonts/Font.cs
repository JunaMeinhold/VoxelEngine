using HexaEngine.IO;
using HexaEngine.Resources;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace HexaEngine.Fonts
{
    public class Font : Resource
    {
        // Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct Character
        {
            public float Left, Right;

            public int Size;
        }

        // Properties.
        public List<Character> FontCharacters { get; private set; }

        public Texture Texture { get; private set; }

        public IView View { get; set; }

        public Font(string path)
        {
            using var fs = FileSystem.Open(ResourceManager.CurrentFontPath + path);
            FontCharacters = new();
            FontCharacters.AddRange(fs.Read<Character>());
            Texture = new();
            Texture.Load(DeviceManager.Current.ID3D11Device, path, fs.Read(fs.Length - fs.Position));
        }

        protected override void Dispose(bool disposing)
        {
            // Release the font texture.
            ReleaseTexture();

            // Release the font data.
            ReleaseFontData();
            base.Dispose(disposing);
        }

        private void ReleaseFontData()
        {
            // Release the font data array.
            FontCharacters?.Clear();
            FontCharacters = null;
        }

        private void ReleaseTexture()
        {
            // Release the texture object.
            Texture?.Dispose();
            Texture = null;
        }

        public void BuildVertexArray(Text text, float drawX, float drawY)
        {
            BuildVertexArray(out var vertices, text.TextString, drawX, drawY);

            #region Vertex Buffer

            Shader.SWrite(DeviceManager.Current, text.VertexBuffer, vertices.ToArray());

            #endregion Vertex Buffer

            vertices?.Clear();
        }

        private void BuildVertexArray(out List<Text.Vertex> vertices, string sentence, float drawX, float drawY)
        {
            // Create list of the vertices
            vertices = new List<Text.Vertex>();

            // Draw each letter onto a quad.
            foreach (var ch in sentence)
            {
                var letter = ch - 32;

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

        private void BuildVertexArray(List<Text.Vertex> vertices, int letter, ref float drawX, ref float drawY)
        {
            // First triangle in the quad
            vertices.Add // Top left.
            (
                new Text.Vertex()
                {
                    Position = new Vector3(drawX, drawY, 0),
                    Texture = new Vector2(FontCharacters[letter].Left, 0)
                }
            );
            vertices.Add // Bottom right.
            (
                new Text.Vertex()
                {
                    Position = new Vector3(drawX + FontCharacters[letter].Size, drawY - 16, 0),
                    Texture = new Vector2(FontCharacters[letter].Right, 1)
                }
            );
            vertices.Add // Bottom left.
            (
                new Text.Vertex()
                {
                    Position = new Vector3(drawX, drawY - 16, 0),
                    Texture = new Vector2(FontCharacters[letter].Left, 1)
                }
            );
            // Second triangle in quad.
            vertices.Add // Top left.
            (
                new Text.Vertex()
                {
                    Position = new Vector3(drawX, drawY, 0),
                    Texture = new Vector2(FontCharacters[letter].Left, 0)
                }
            );
            vertices.Add // Top right.
            (
                new Text.Vertex()
                {
                    Position = new Vector3(drawX + FontCharacters[letter].Size, drawY, 0),
                    Texture = new Vector2(FontCharacters[letter].Right, 0)
                }
            );
            vertices.Add // Bottom right.
            (
                new Text.Vertex()
                {
                    Position = new Vector3(drawX + FontCharacters[letter].Size, drawY - 16, 0),
                    Texture = new Vector2(FontCharacters[letter].Right, 1)
                }
            );
        }

        public void Render(ID3D11DeviceContext context)
        {
            context.PSSetShaderResource(0, Texture);
        }
    }
}