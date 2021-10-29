using HexaEngine.Resources;
using HexaEngine.Scenes.Interfaces;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace HexaEngine.Fonts
{
    public class Font : Resource
    {
        private static FontShader fontShader;

        // Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct Character
        {
            // Variables.
            public float Left, Right;

            public int Size;

            // Constructor
            public Character(string fontData)
            {
                var ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.CurrencyDecimalSeparator = ".";
                var data = fontData.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                Left = float.Parse(data[^3], NumberStyles.Any, ci);
                Right = float.Parse(data[^2], NumberStyles.Any, ci);
                Size = int.Parse(data[^1], NumberStyles.Any, ci);
            }
        }

        // Properties.
        public List<Character> FontCharacters { get; private set; }

        public Texture Texture { get; private set; }

        public IView View { get; set; }

        public Font(string fontFileName, string textureFileName)
        {
            if (fontShader is null)
                fontShader = ResourceManager.LoadShader<FontShader>();
            // Load in the text file containing the font data.
            LoadFontData(fontFileName);

            // Load the texture that has font characters on it.
            LoadTexture(textureFileName);
        }

        protected override void Dispose(bool disposing)
        {
            // Release the font texture.
            ReleaseTexture();

            // Release the font data.
            ReleaseFontData();
            base.Dispose(disposing);
        }

        private bool LoadFontData(string fontFileName)
        {
            try
            {
                // Get all the lines containing the font data.
                var fontDataLines = File.ReadAllLines(fontFileName);

                // Create Font and fill with characters.
                FontCharacters = new List<Character>();
                foreach (var line in fontDataLines)
                    FontCharacters.Add(new Character(line));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ReleaseFontData()
        {
            // Release the font data array.
            FontCharacters?.Clear();
            FontCharacters = null;
        }

        private bool LoadTexture(string textureFileName)
        {
            // Create new texture object.
            Texture = ResourceManager.LoadTexture(textureFileName);
            return true;
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

        public void Render(Text text, Matrix4x4? baseMatrix = null)
        {
            fontShader.View = View;
            // Set the vertex buffer to active in the input assembler so it can be rendered.
            DeviceManager.Current.ID3D11DeviceContext.IASetVertexBuffers(0, new VertexBufferView(text.VertexBuffer, Marshal.SizeOf<Text.Vertex>(), 0));

            // Set the index buffer to active in the input assembler so it can be rendered.
            DeviceManager.Current.ID3D11DeviceContext.IASetIndexBuffer(text.IndexBuffer, Format.R32_UInt, 0);

            // Set the type of the primitive that should be rendered from this vertex buffer, in this case triangles.
            DeviceManager.Current.ID3D11DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            fontShader.Render(DeviceManager.Current.ID3D11DeviceContext, text.IndexCount, baseMatrix ?? Matrix4x4.Identity, Texture.TextureResource, text.Color);
        }
    }
}