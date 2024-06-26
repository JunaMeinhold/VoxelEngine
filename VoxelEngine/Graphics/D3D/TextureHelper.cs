namespace VoxelEngine.Rendering.D3D
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Hexa.NET.DirectXTex;
    using Silk.NET.Core.Native;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.IO;
    using Format = Vortice.DXGI.Format;

    public static class TextureHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeMipLevels(int width, int height)
        {
            return (int)MathF.Log2(MathF.Max(width, height));
        }

        public static bool GenerateMipMaps { get; set; } = true;

        public static unsafe ID3D11Texture1D LoadTexture1DFile(ID3D11Device device, string path, bool postProcess = true)
        {
            ScratchImage scratchImage = LoadFromFile(path);
            if (postProcess)
            {
                ProcessTexture(ref scratchImage);
            }

            Silk.NET.Direct3D11.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)device.NativePointer,
                scratchImage,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            scratchImage.Release();
            return new((nint)res);
        }

        public static unsafe ID3D11Texture2D LoadTexture2DFile(ID3D11Device device, string path, bool postProcess = true)
        {
            ScratchImage scratchImage = LoadFromFile(path);
            if (postProcess)
            {
                ProcessTexture(ref scratchImage);
            }

            Silk.NET.Direct3D11.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)device.NativePointer,
                scratchImage,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            scratchImage.Release();
            return new((nint)res);
        }

        public static unsafe ID3D11Texture3D LoadTexture3DFile(ID3D11Device device, string path, bool postProcess = true)
        {
            ScratchImage scratchImage = LoadFromFile(path);
            if (postProcess)
            {
                ProcessTexture(ref scratchImage);
            }

            Silk.NET.Direct3D11.ID3D11Resource* res;
            DirectXTex.CreateTextureEx2(
                (Silk.NET.Direct3D11.ID3D11Device*)device.NativePointer,
                scratchImage,
                (uint)ResourceUsage.Immutable,
                (uint)BindFlags.ShaderResource,
                (uint)CpuAccessFlags.None,
                (uint)ResourceOptionFlags.None,
                CreateTexFlags.Default,
                &res);

            scratchImage.Release();
            return new((nint)res);
        }

        private static void SwapTexture(ref ScratchImage target, ScratchImage newTex)
        {
            target.Release();
            target = newTex;
        }

        private static unsafe void ProcessTexture(ref ScratchImage image)
        {
            TexMetadata metadata = image.GetMetadata();

            HResult result;
            if (GenerateMipMaps && metadata.MipLevels == 1)
            {
                ScratchImage image1 = DirectXTex.CreateScratchImage();
                result = DirectXTex.GenerateMipMaps2(image.GetImages(), image.GetImageCount(), image.GetMetadata(), TexFilterFlags.Default, (nuint)ComputeMipLevels((int)metadata.Width, (int)metadata.Height), image1);

                if (!result.IsSuccess)
                {
                    result.Throw();
                }

                SwapTexture(ref image, image1);
            }
        }

        /// <summary>
        /// Loads a Texture2DArray from image files.<br/>
        /// Automatically selects WIC, DDS, TGA, HDR after the file extension.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
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
    }
}