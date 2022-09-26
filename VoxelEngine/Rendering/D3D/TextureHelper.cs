namespace VoxelEngine.Rendering.D3D
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using DirectXTexNet;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using Vortice.Textures;
    using VoxelEngine.Core;
    using VoxelEngine.IO;
    using static VoxelEngine.Mathematics.Noise.FastNoise;
    using Format = Vortice.DXGI.Format;

    public static class TextureHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMipLevel(int width, int height)
        {
            return (int)(1 + Math.Floor(Math.Log2(Math.Max(width, height))));
        }

        /// <summary>
        /// Loads a Texture2D from a file. <br/>
        /// Automatically selects WIC, DDS, TGA, HDR after the file extension.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ID3D11Texture2D LoadFromFile(ID3D11Device device, string path)
        {
            Load(device, path);
            string extension = Path.GetExtension(path);
            return extension switch
            {
                ".dds" => LoadFromDDSFile(device, path),
                ".tga" => LoadFromTGAFile(device, path),
                ".hdr" => LoadFromHDRFile(device, path),
                _ => LoadFromWICFile(device, path),
            };
        }

        public static ID3D11Texture2D Load(ID3D11Device device, string path)
        {
            return null;
        }

        /// <summary>
        /// Loads a Texture2DArray from image files.<br/>
        /// Automatically selects WIC, DDS, TGA, HDR after the file extension.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ID3D11Texture2D LoadFromFiles(ID3D11Device device, string[] paths)
        {
            ScratchImage[] textures = new ScratchImage[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ScratchImage tex = LoadFromFile(Paths.CurrentTexturePath + paths[i]);
                textures[i] = tex;
            }

            TexMetadata meta = textures[0].GetMetadata();
            Texture2DDescription desc = new()
            {
                Width = meta.Width,
                Height = meta.Height,
                ArraySize = paths.Length,
                BindFlags = BindFlags.ShaderResource,
                Usage = ResourceUsage.Immutable,
                CPUAccessFlags = 0,
                Format = (Format)meta.Format,
                MipLevels = meta.MipLevels,
                MiscFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
            };

            SubresourceData[] subresources = new SubresourceData[textures.Length * meta.MipLevels];
            int a = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                for (int j = 0; j < meta.MipLevels; j++)
                {
                    Image img = textures[i].GetImage(j);
                    subresources[a++] = new(img.Pixels, (int)img.RowPitch, (int)img.SlicePitch);
                }
            }

            ID3D11Texture2D texture = device.CreateTexture2D(desc, subresources);

            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Dispose();
            }

            return texture;
        }

        public static ScratchImage LoadFromFile(string path)
        {
            string extension = Path.GetExtension(path);
            return extension switch
            {
                ".dds" => LoadFromDDSFile(path),
                ".tga" => LoadFromTGAFile(path),
                ".hdr" => LoadFromHDRFile(path),
                _ => LoadFromWICFile(path),
            };
        }

        public static ScratchImage LoadFromWICFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = TexHelper.Instance.LoadFromWICMemory(ptr, fs.Length, WIC_FLAGS.NONE);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static ScratchImage LoadFromDDSFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = TexHelper.Instance.LoadFromDDSMemory(ptr, fs.Length, DDS_FLAGS.NONE);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static ScratchImage LoadFromTGAFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = TexHelper.Instance.LoadFromTGAMemory(ptr, fs.Length);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static ScratchImage LoadFromHDRFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = TexHelper.Instance.LoadFromHDRMemory(ptr, fs.Length);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        /// <summary>
        /// Loads a Texture2D from a WIC image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ID3D11Texture2D LoadFromWICFile(ID3D11Device device, string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = TexHelper.Instance.LoadFromWICMemory(ptr, fs.Length, WIC_FLAGS.NONE);
            TexMetadata metadata = image.GetMetadata();

            ScratchImage image1 = image.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, GetMipLevel(metadata.Width, metadata.Height));
            image.Dispose();
            if (metadata.Format != DXGI_FORMAT.R8G8B8A8_UNORM)
            {
                image = image1.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0);
                image1.Dispose();
            }
            else
            {
                image = image1;
            }

            ID3D11Texture2D texture = image.CreateTextureEx(
                device,
                ResourceUsage.Immutable,
                BindFlags.ShaderResource,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                false);
            image.Dispose();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return texture;
        }

        /// <summary>
        /// Loads a Texture2D from a DDS image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ID3D11Texture2D LoadFromDDSFile(ID3D11Device device, string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = TexHelper.Instance.LoadFromDDSMemory(ptr, fs.Length, DDS_FLAGS.NONE);
            TexMetadata metadata = image.GetMetadata();

            ID3D11Texture2D texture = image.CreateTextureEx(
                device,
                ResourceUsage.Immutable,
                BindFlags.ShaderResource,
                CpuAccessFlags.None,
                metadata.IsCubemap() ? ResourceOptionFlags.TextureCube : ResourceOptionFlags.None,
                false);
            image.Dispose();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return texture;
        }

        /// <summary>
        /// Loads a Texture2D from a TGA image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ID3D11Texture2D LoadFromTGAFile(ID3D11Device device, string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = TexHelper.Instance.LoadFromTGAMemory(ptr, fs.Length);
            TexMetadata metadata = image.GetMetadata();

            image.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, GetMipLevel(metadata.Width, metadata.Height));
            ID3D11Texture2D texture = image.CreateTextureEx(
                device,
                ResourceUsage.Immutable,
                BindFlags.ShaderResource,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                false);
            image.Dispose();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return texture;
        }

        /// <summary>
        /// Loads a Texture2D from a HDR image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ID3D11Texture2D LoadFromHDRFile(ID3D11Device device, string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = TexHelper.Instance.LoadFromHDRMemory(ptr, fs.Length);
            TexMetadata metadata = image.GetMetadata();

            image.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, GetMipLevel(metadata.Width, metadata.Height));
            ID3D11Texture2D texture = image.CreateTextureEx(
                device,
                ResourceUsage.Immutable,
                BindFlags.ShaderResource,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                false);
            image.Dispose();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return texture;
        }
    }
}