namespace VoxelEngine.Rendering.D3D
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using HexaEngine.DirectXTex;
    using SharpGen.Runtime;
    using Silk.NET.Core.Native;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.IO;
    using Format = Vortice.DXGI.Format;

    public static class TextureHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMipLevel(int width, int height)
        {
            return (int)MathF.Log2(MathF.Max(width, height));
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
        public static unsafe ID3D11Texture2D LoadFromFiles(ID3D11Device device, string[] paths)
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
                Width = (int)meta.Width,
                Height = (int)meta.Height,
                ArraySize = paths.Length,
                BindFlags = BindFlags.ShaderResource,
                Usage = ResourceUsage.Immutable,
                CPUAccessFlags = 0,
                Format = (Format)meta.Format,
                MipLevels = (int)meta.MipLevels,
                MiscFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
            };

            SubresourceData[] subresources = new SubresourceData[(textures.Length * (int)meta.MipLevels)];
            int a = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                for (int j = 0; j < (int)meta.MipLevels; j++)
                {
                    Image img = textures[i].GetImage((nuint)j, 0, 0);
                    subresources[a++] = new(img.Pixels, (int)img.RowPitch, (int)img.SlicePitch);
                }
            }

            ID3D11Texture2D texture = device.CreateTexture2D(desc, subresources);

            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Release();
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

        public static unsafe ScratchImage LoadFromWICFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromWICMemory((void*)ptr, (nuint)fs.Length, WICFlags.None, null, image, default);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static unsafe ScratchImage LoadFromDDSFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromDDSMemory((void*)ptr, (nuint)fs.Length, DDSFlags.None, null, image);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static unsafe ScratchImage LoadFromTGAFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromTGAMemory((void*)ptr, (nuint)fs.Length, TGAFlags.None, null, image);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        public static unsafe ScratchImage LoadFromHDRFile(string path)
        {
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromHDRMemory((void*)ptr, (nuint)fs.Length, null, image);
            Marshal.FreeHGlobal(ptr);
            return image;
        }

        /// <summary>
        /// Loads a Texture2D from a WIC image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ID3D11Texture2D LoadFromWICFile(ID3D11Device device, string path)
        {
            HResult result;
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromWICMemory((void*)ptr, (nuint)fs.Length, WICFlags.None, null, image, default);
            TexMetadata metadata = image.GetMetadata();

            if (metadata.MipLevels == 1)
            {
                ScratchImage image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.GenerateMipMaps2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), TexFilterFlags.Default, (nuint)GetMipLevel((int)metadata.Width, (int)metadata.Height), image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
            }

            if (metadata.Format != (int)Format.R8G8B8A8_UNorm)
            {
                var image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.Convert2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), (int)Format.R8G8B8A8_UNorm, TexFilterFlags.Default, 1, image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
            }

            Silk.NET.Direct3D11.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)device.NativePointer,
                image,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            image.Release();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return new((nint)res);
        }

        /// <summary>
        /// Loads a Texture2D from a DDS image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ID3D11Texture2D LoadFromDDSFile(ID3D11Device device, string path)
        {
            HResult result;
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = DirectXTex.CreateScratchImage();
            result = DirectXTex.LoadFromDDSMemory((void*)ptr, (nuint)fs.Length, DDSFlags.None, null, image);

            if (!result.IsSuccess)
            {
                result.Throw();
            }

            TexMetadata metadata = image.GetMetadata();

            if (metadata.MipLevels == 1)
            {
                ScratchImage image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.GenerateMipMaps2(image.GetImages(), image.GetImageCount(), metadata, TexFilterFlags.Default, (nuint)GetMipLevel((int)metadata.Width, (int)metadata.Height), image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
                metadata = image.GetMetadata();
            }

            /*
            if (metadata.Format != (int)Format.R8G8B8A8_UNorm)
            {
                var image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.Convert2(image.GetImages(), image.GetImageCount(), metadata, (int)Format.R8G8B8A8_UNorm, TexFilterFlags.Default, 1, image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
                metadata = image.GetMetadata();
            }*/

            var pt = (void*)device.NativePointer;
            Silk.NET.Direct3D11.ID3D11Resource* res;
            result = DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)pt,
                image,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            if (!result.IsSuccess)
            {
                result.Throw();
            }

            image.Release();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return new((nint)res);
        }

        /// <summary>
        /// Loads a Texture2D from a TGA image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ID3D11Texture2D LoadFromTGAFile(ID3D11Device device, string path)
        {
            HResult result;
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromTGAMemory((void*)ptr, (nuint)fs.Length, TGAFlags.None, null, image);
            TexMetadata metadata = image.GetMetadata();

            if (metadata.MipLevels == 1)
            {
                ScratchImage image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.GenerateMipMaps2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), TexFilterFlags.Default, (nuint)GetMipLevel((int)metadata.Width, (int)metadata.Height), image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
            }

            if (metadata.Format != (int)Format.R8G8B8A8_UNorm)
            {
                var image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.Convert2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), (int)Format.R8G8B8A8_UNorm, TexFilterFlags.Default, 1, image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
            }

            Silk.NET.Direct3D11.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)device.NativePointer,
                image,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            image.Release();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return new((nint)res);
        }

        /// <summary>
        /// Loads a Texture2D from a HDR image file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ID3D11Texture2D LoadFromHDRFile(ID3D11Device device, string path)
        {
            HResult result;
            VirtualStream fs = FileSystem.Open(path);
            IntPtr ptr = fs.GetIntPtr(out _);

            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.LoadFromHDRMemory((void*)ptr, (nuint)fs.Length, null, image);
            TexMetadata metadata = image.GetMetadata();

            if (metadata.MipLevels == 1)
            {
                ScratchImage image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.GenerateMipMaps2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), TexFilterFlags.Default, (nuint)GetMipLevel((int)metadata.Width, (int)metadata.Height), image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
            }

            if (metadata.Format != (int)Format.R8G8B8A8_UNorm)
            {
                var image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.Convert2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), (int)Format.R8G8B8A8_UNorm, TexFilterFlags.Default, 1, image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                image.Release();
                image = image1;
            }

            Silk.NET.Direct3D11.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)device.NativePointer,
                image,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            image.Release();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return new((nint)res);
        }
    }
}