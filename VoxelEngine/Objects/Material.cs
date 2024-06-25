namespace VoxelEngine.Objects
{
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Shaders;
    using VoxelEngine.Rendering.D3D;

    public class Material : IDisposable
    {
        private bool disposedValue;

        public string Name { get; set; }

        public Vector3 AmbientColor { get; set; }

        public Vector3 DiffuseColor { get; set; }

        public Vector3 SpecularColor { get; set; }

        public float SpecularCoefficient { get; set; }

        public float Transparency { get; set; }

        public Texture2D Ambient { get; private set; }

        public Texture2D Diffuse { get; private set; }

        public Texture2D Specular { get; private set; }

        public Texture2D SpecularHighlight { get; private set; }

        public Texture2D Bump { get; private set; }

        public Texture2D Displacement { get; private set; }

        public Texture2D StencilDecal { get; private set; }

        public Texture2D AlphaTexture { get; private set; }

        public Texture2D MetallicTexture { get; private set; }

        public Texture2D RoughnessTexture { get; private set; }

        public string AmbientTextureMap { get; set; }

        public string DiffuseTextureMap { get; set; }

        public string SpecularTextureMap { get; set; }

        public string SpecularHighlightTextureMap { get; set; }

        public string BumpMap { get; set; }

        public string DisplacementMap { get; set; }

        public string StencilDecalMap { get; set; }

        public string AlphaTextureMap { get; set; }

        public string MetallicTextureMap { get; set; }

        public string RoughnessTextureMap { get; set; }

        public ID3D11SamplerState SamplerState { get; set; }

        public void Bind(ID3D11DeviceContext context)
        {
            Ambient?.Bind(context, 0, ShaderStage.Pixel);
            Diffuse?.Bind(context, 1, ShaderStage.Pixel);
            Specular?.Bind(context, 2, ShaderStage.Pixel);
            SpecularHighlight?.Bind(context, 3, ShaderStage.Pixel);
            Bump?.Bind(context, 4, ShaderStage.Pixel);
            Displacement?.Bind(context, 5, ShaderStage.Pixel);
            StencilDecal?.Bind(context, 6, ShaderStage.Pixel);
            AlphaTexture?.Bind(context, 7, ShaderStage.Pixel);
            MetallicTexture?.Bind(context, 8, ShaderStage.Pixel);
            RoughnessTexture?.Bind(context, 9, ShaderStage.Pixel);
            Displacement?.Bind(context, 0, ShaderStage.Domain);
            context.PSSetSampler(0, SamplerState);
            context.DSSetSampler(0, SamplerState);
        }

        public void Initialize(ID3D11Device device)
        {
            if (AmbientTextureMap != null)
            {
                Ambient = new(device, AmbientTextureMap);
            }
            if (DiffuseTextureMap != null)
            {
                Diffuse = new(device, DiffuseTextureMap);
            }
            if (SpecularTextureMap != null)
            {
                Specular = new(device, SpecularTextureMap);
            }
            if (SpecularHighlightTextureMap != null)
            {
                SpecularHighlight = new(device, SpecularHighlightTextureMap);
            }
            if (BumpMap != null)
            {
                Bump = new(device, BumpMap);
            }
            if (DisplacementMap != null)
            {
                Displacement = new(device, DisplacementMap);
            }
            if (StencilDecalMap != null)
            {
                StencilDecal = new(device, StencilDecalMap);
            }
            if (AlphaTextureMap != null)
            {
                AlphaTexture = new(device, AlphaTextureMap);
            }
            if (MetallicTextureMap != null)
            {
                MetallicTexture = new(device, MetallicTextureMap);
            }
            if (RoughnessTextureMap != null)
            {
                RoughnessTexture = new(device, RoughnessTextureMap);
            }
        }

        public static implicit operator MaterialDesc(Material material)
        {
            return new()
            {
                AmbientColor = material.AmbientColor,
                DiffuseColor = material.DiffuseColor,
                SpecularColor = material.SpecularColor,
                SpecularCoefficient = material.SpecularCoefficient,
                Transparency = material.Transparency,
                HasAmbientTextureMap = material.Ambient is not null ? 1 : 0,
                HasDiffuseTextureMap = material.Diffuse is not null ? 1 : 0,
                HasSpecularTextureMap = material.Specular is not null ? 1 : 0,
                HasSpecularHighlightTextureMap = material.SpecularHighlight is not null ? 1 : 0,
                HasBumpMap = material.Bump is not null ? 1 : 0,
                HasDisplacementMap = material.Displacement is not null ? 1 : 0,
                HasStencilDecalMap = material.StencilDecal is not null ? 1 : 0,
                HasAlphaTextureMap = material.AlphaTexture is not null ? 1 : 0,
                HasMetallicMap = material.MetallicTexture is not null ? 1 : 0,
                HasRoughnessMap = material.RoughnessTexture is not null ? 1 : 0,
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Ambient?.Dispose();
                Diffuse?.Dispose();
                Specular?.Dispose();
                SpecularHighlight?.Dispose();
                Bump?.Dispose();
                Displacement?.Dispose();
                StencilDecal?.Dispose();
                AlphaTexture?.Dispose();
                MetallicTexture?.Dispose();
                RoughnessTexture?.Dispose();
                disposedValue = true;
            }
        }

        ~Material()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MaterialDesc
    {
        public Vector3 AmbientColor;
        public float reserved0;
        public Vector3 DiffuseColor;
        public float reserved1;
        public Vector3 SpecularColor;
        public float reserved2;
        public float SpecularCoefficient;
        public float Transparency;
        public float reserved3;
        public float reserved4;

        public int HasAmbientTextureMap;
        public int HasDiffuseTextureMap;
        public int HasSpecularTextureMap;
        public int HasSpecularHighlightTextureMap;
        public int HasBumpMap;
        public int HasDisplacementMap;
        public int HasStencilDecalMap;
        public int HasAlphaTextureMap;
        public int HasMetallicMap;
        public int HasRoughnessMap;

        public float reserved7;
        public float reserved8;
    }
}