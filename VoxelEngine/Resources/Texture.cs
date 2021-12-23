using HexaEngine.IO;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.WIC;

namespace HexaEngine.Resources
{
    public class Texture : Resource
    {
        public static SamplerDescription DefaultSamplerDescription { get; set; } = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            ComparisonFunction = ComparisonFunction.Always,
            BorderColor = (Vortice.Mathematics.Color4)Color.FromArgb(0, 0, 0, 0),  // Black Border.
            MinLOD = 0,
            MaxLOD = float.MaxValue
        };

        // Properties
        public ID3D11Texture2D Texture2D { get; private set; }

        public ID3D11ShaderResourceView TextureResource { get; private set; }

        public ID3D11SamplerState SamplerState { get; private set; }

        public SamplerDescription SamplerDescription { get; set; } = DefaultSamplerDescription;

        public static implicit operator ID3D11ShaderResourceView(Texture texture)
        {
            return texture?.TextureResource ?? null;
        }

        public void Render(ID3D11DeviceContext context, int slot = 0)
        {
            context.PSSetShaderResource(slot, TextureResource);
            context.PSSetSampler(slot, SamplerState);
        }

        public void UpdateSampler(ID3D11Device device)
        {
            SamplerState?.Dispose();
            SamplerState = device.CreateSamplerState(SamplerDescription);
        }

        public void Load(ID3D11Device device, string name, byte[] data)
        {
            Texture2D = LoadFromFile(device, data);
            Texture2D.DebugName = "Texture: " + name;
            ShaderResourceViewDescription srvDesc = new()
            {
                Format = Texture2D.Description.Format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D
            };
            srvDesc.Texture2D.MostDetailedMip = 0;
            srvDesc.Texture2D.MipLevels = -1;

            TextureResource = device.CreateShaderResourceView(Texture2D, srvDesc);
            TextureResource.DebugName = nameof(TextureResource) + ": " + name;
            device.ImmediateContext.GenerateMips(TextureResource);
            SamplerState = device.CreateSamplerState(SamplerDescription);
        }

        public void Load(ID3D11Device device, string fileName)
        {
            Texture2D = LoadFromFile(device, fileName);
            Texture2D.DebugName = "Texture: " + Path.GetFileName(fileName);
            ShaderResourceViewDescription srvDesc = new()
            {
                Format = Texture2D.Description.Format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D
            };
            srvDesc.Texture2D.MostDetailedMip = 0;
            srvDesc.Texture2D.MipLevels = -1;

            TextureResource = device.CreateShaderResourceView(Texture2D, srvDesc);
            TextureResource.DebugName = nameof(TextureResource) + ": " + Path.GetFileName(fileName);
            device.ImmediateContext.GenerateMips(TextureResource);
            SamplerState = device.CreateSamplerState(SamplerDescription);
        }

        public void Load(ID3D11Device device, string[] fileName)
        {
            Texture2D = LoadFromFiles(device, fileName);
            Texture2D.DebugName = "Texture: " + string.Join(' ', fileName);
            ShaderResourceViewDescription srvDesc = new()
            {
                Format = Texture2D.Description.Format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2DArray
            };
            srvDesc.Texture2DArray.MostDetailedMip = 0;
            srvDesc.Texture2DArray.MipLevels = -1;
            srvDesc.Texture2DArray.ArraySize = fileName.Length;

            TextureResource = device.CreateShaderResourceView(Texture2D, srvDesc);
            TextureResource.DebugName = nameof(TextureResource) + ": " + string.Join(' ', fileName);
            device.ImmediateContext.GenerateMips(TextureResource);
            SamplerState = device.CreateSamplerState(SamplerDescription);
        }

        public void LoadCubeMap(ID3D11Device device, string fileName)
        {
            Texture2D = LoadCubeMapFromFile(device, fileName);
            Texture2D.DebugName = "Texture: " + Path.GetFileName(fileName);
            ShaderResourceViewDescription srvDesc = new()
            {
                Format = Texture2D.Description.Format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.TextureCube
            };
            srvDesc.Texture2D.MostDetailedMip = 0;
            srvDesc.Texture2D.MipLevels = -1;

            TextureResource = device.CreateShaderResourceView(Texture2D, srvDesc);
            TextureResource.DebugName = nameof(TextureResource) + ": " + Path.GetFileName(fileName);
            device.ImmediateContext.GenerateMips(TextureResource);
            SamplerState = device.CreateSamplerState(SamplerDescription);
        }

        protected override void Dispose(bool disposing)
        {
            SamplerState?.Dispose();
            SamplerState = null;
            TextureResource?.Dispose();
            TextureResource = null;
            Texture2D?.Dispose();
            Texture2D = null;
            base.Dispose(disposing);
        }

        public static ID3D11Texture2D LoadFromFile(ID3D11Device device, byte[] bytes)
        {
            bool imageConverted = false;
            IWICImagingFactory factory = new();
            IWICBitmapDecoder decoder;
            IWICBitmapFrameDecode frame;
            IWICFormatConverter converter = null;
            var fs = new MemoryStream(bytes);
            decoder = factory.CreateDecoderFromStream(fs, DecodeOptions.CacheOnDemand);
            frame = decoder.GetFrame(0);

            var dxgiFormat = GetDXGIFormatFromWICFormat(frame.PixelFormat);

            if (dxgiFormat == Format.Unknown)
            {
                var format = GetConvertToWICFormat(frame.PixelFormat);
                if (format == PixelFormat.FormatDontCare) throw new NotSupportedException(frame.PixelFormat.ToString());
                dxgiFormat = GetDXGIFormatFromWICFormat(format);
                converter = factory.CreateFormatConverter();

                if (!converter.CanConvert(frame.PixelFormat, format))
                    throw new NotSupportedException(frame.PixelFormat.ToString());

                converter.Initialize(frame, format, BitmapDitherType.ErrorDiffusion, null, 0, BitmapPaletteType.Custom);
                imageConverted = true;
            }

            int bitsPerPixel = GetDXGIFormatBitsPerPixel(dxgiFormat);
            int bytesPerRow = frame.Size.Width * bitsPerPixel / 8;
            int imageSize = bytesPerRow * frame.Size.Height;

            IntPtr data = Marshal.AllocHGlobal(imageSize);
            if (imageConverted)
            {
                converter.CopyPixels(bytesPerRow, imageSize, data);
            }
            else
            {
                frame.CopyPixels(bytesPerRow, imageSize, data);
            }

            var desc = new Texture2DDescription()
            {
                Width = frame.Size.Width,
                Height = frame.Size.Height,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = dxgiFormat,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.GenerateMips,
                SampleDescription = new SampleDescription(1, 0),
            };
            var subres = new SubresourceData[] { new SubresourceData(data, bytesPerRow) };
            var texture = device.CreateTexture2D(desc, subres);

            Marshal.FreeHGlobal(data);

            converter?.Dispose();
            frame.Dispose();
            decoder.Dispose();
            factory.Dispose();
            fs.Dispose();

            return texture;
        }

        public static ID3D11Texture2D LoadFromFile(ID3D11Device device, string path)
        {
            bool imageConverted = false;
            IWICImagingFactory factory = new();
            IWICBitmapDecoder decoder;
            IWICBitmapFrameDecode frame;
            IWICFormatConverter converter = null;
            var fs = FileSystem.Open(path);
            decoder = factory.CreateDecoderFromStream(fs, DecodeOptions.CacheOnDemand);
            frame = decoder.GetFrame(0);

            var dxgiFormat = GetDXGIFormatFromWICFormat(frame.PixelFormat);

            if (dxgiFormat == Format.Unknown)
            {
                var format = GetConvertToWICFormat(frame.PixelFormat);
                if (format == PixelFormat.FormatDontCare) throw new NotSupportedException(frame.PixelFormat.ToString());
                dxgiFormat = GetDXGIFormatFromWICFormat(format);
                converter = factory.CreateFormatConverter();

                if (!converter.CanConvert(frame.PixelFormat, format))
                    throw new NotSupportedException(frame.PixelFormat.ToString());

                converter.Initialize(frame, format, BitmapDitherType.ErrorDiffusion, null, 0, BitmapPaletteType.Custom);
                imageConverted = true;
            }

            int bitsPerPixel = GetDXGIFormatBitsPerPixel(dxgiFormat);
            int bytesPerRow = frame.Size.Width * bitsPerPixel / 8;
            int imageSize = bytesPerRow * frame.Size.Height;

            IntPtr data = Marshal.AllocHGlobal(imageSize);
            if (imageConverted)
            {
                converter.CopyPixels(bytesPerRow, imageSize, data);
            }
            else
            {
                frame.CopyPixels(bytesPerRow, imageSize, data);
            }

            var desc = new Texture2DDescription()
            {
                Width = frame.Size.Width,
                Height = frame.Size.Height,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = dxgiFormat,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.GenerateMips,
                SampleDescription = new SampleDescription(1, 0),
            };
            var subres = new SubresourceData[] { new SubresourceData(data, bytesPerRow) };
            var texture = device.CreateTexture2D(desc, subres);

            Marshal.FreeHGlobal(data);

            converter?.Dispose();
            frame.Dispose();
            decoder.Dispose();
            factory.Dispose();
            fs.Dispose();

            return texture;
        }

        public static ID3D11Texture2D LoadCubeMapFromFile(ID3D11Device device, string path)
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Trace.WriteLine(folder);
            var fs = FileSystem.Open(path);
            var ptr = fs.GetIntPtr();

            var image = DirectXTexNet.TexHelper.Instance.LoadFromDDSMemory(ptr, fs.Length, DirectXTexNet.DDS_FLAGS.NONE);
            var image1 = image.Decompress(Format.R16G16B16A16_Float);
            var texture = image1.CreateTextureEx(
                device,
                ResourceUsage.Default,
                BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags.None,
                ResourceOptionFlags.GenerateMips,
                false);
            image1.Dispose();
            image.Dispose();
            Marshal.FreeHGlobal(ptr);
            fs.Dispose();
            return texture;
        }

        public static ID3D11Texture2D LoadFromFiles(ID3D11Device device, string[] paths)
        {
            Format format = Format.Unknown;
            int width = 0;
            int height = 0;
            SubresourceData[] subresources = new SubresourceData[paths.Length];
            IntPtr[] ptrs = new IntPtr[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                Load(ResourceManager.CurrentTexturePath + paths[i], out width, out height, out format, out var data, out var bytesPerRow);
                subresources[i] = new SubresourceData(data, bytesPerRow);
                ptrs[i] = data;
            }
            var desc = new Texture2DDescription()
            {
                Width = width,
                Height = height,
                ArraySize = paths.Length,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = format,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.GenerateMips,
                SampleDescription = new SampleDescription(1, 0),
            };

            var texture = device.CreateTexture2D(desc, subresources);
            for (var i = 0; i < ptrs.Length; i++)
                Marshal.FreeHGlobal(ptrs[i]);

            return texture;
        }

        private static void Load(string path, out int width, out int height, out Format dxgiFormat, out IntPtr data, out int bytesPerRow)
        {
            bool imageConverted = false;
            IWICImagingFactory factory = new();
            IWICBitmapDecoder decoder;
            IWICBitmapFrameDecode frame;
            IWICFormatConverter converter = null;
            var fs = FileSystem.Open(path);
            decoder = factory.CreateDecoderFromStream(fs, DecodeOptions.CacheOnDemand);
            frame = decoder.GetFrame(0);

            dxgiFormat = GetDXGIFormatFromWICFormat(frame.PixelFormat);

            if (dxgiFormat == Format.Unknown)
            {
                var format = GetConvertToWICFormat(frame.PixelFormat);
                if (format == PixelFormat.FormatDontCare) throw new NotSupportedException(frame.PixelFormat.ToString());
                dxgiFormat = GetDXGIFormatFromWICFormat(format);
                converter = factory.CreateFormatConverter();

                if (!converter.CanConvert(frame.PixelFormat, format))
                    throw new NotSupportedException(frame.PixelFormat.ToString());

                converter.Initialize(frame, format, BitmapDitherType.ErrorDiffusion, null, 0, BitmapPaletteType.Custom);
                imageConverted = true;
            }

            int bitsPerPixel = GetDXGIFormatBitsPerPixel(dxgiFormat);
            bytesPerRow = frame.Size.Width * bitsPerPixel / 8;
            int size = bytesPerRow * frame.Size.Height;

            width = frame.Size.Width;
            height = frame.Size.Height;

            data = Marshal.AllocHGlobal(size);
            if (imageConverted)
            {
                converter.CopyPixels(bytesPerRow, size, data);
            }
            else
            {
                frame.CopyPixels(bytesPerRow, size, data);
            }
            converter?.Dispose();
            frame.Dispose();
            decoder.Dispose();
            factory.Dispose();
            fs.Dispose();
        }

        // get the number of bits per pixel for a dxgi format
        public static int GetDXGIFormatBitsPerPixel(Format dxgiFormat)
        {
            return dxgiFormat switch
            {
                Format.R32G32B32A32_Float => 128,
                Format.R16G16B16A16_Float => 64,
                Format.R16G16B16A16_UNorm => 64,
                Format.R8G8B8A8_UNorm => 32,
                Format.B8G8R8A8_UNorm => 32,
                Format.B8G8R8X8_UNorm => 32,
                Format.R10G10B10_Xr_Bias_A2_UNorm => 32,
                Format.R10G10B10A2_UNorm => 32,
                Format.B5G5R5A1_UNorm => 16,
                Format.B5G6R5_UNorm => 16,
                Format.R32_Float => 32,
                Format.R16_Float => 16,
                Format.R16_UNorm => 16,
                Format.R8_UNorm => 8,
                Format.A8_UNorm => 8,
                _ => 0
            };
        }

        // get a dxgi compatible wic format from another wic format
        public static Guid GetConvertToWICFormat(Guid wicFormatGUID)
        {
            if (wicFormatGUID == PixelFormat.FormatBlackWhite) return PixelFormat.Format8bppGray;
            else if (wicFormatGUID == PixelFormat.Format1bppIndexed) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format2bppIndexed) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format4bppIndexed) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format8bppIndexed) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format2bppGray) return PixelFormat.Format8bppGray;
            else if (wicFormatGUID == PixelFormat.Format4bppGray) return PixelFormat.Format8bppGray;
            else if (wicFormatGUID == PixelFormat.Format16bppGrayFixedPoint) return PixelFormat.Format16bppGrayHalf;
            else if (wicFormatGUID == PixelFormat.Format32bppGrayFixedPoint) return PixelFormat.Format32bppGrayFloat;
            else if (wicFormatGUID == PixelFormat.Format16bppBGR555) return PixelFormat.Format16bppBGRA5551;
            else if (wicFormatGUID == PixelFormat.Format32bppBGR101010) return PixelFormat.Format32bppRGBA1010102;
            else if (wicFormatGUID == PixelFormat.Format24bppBGR) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format24bppRGB) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format32bppPBGRA) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format32bppPRGBA) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format48bppRGB) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format48bppBGR) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format64bppBGRA) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format64bppPRGBA) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format64bppPBGRA) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format48bppRGBFixedPoint) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format48bppBGRFixedPoint) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format64bppRGBAFixedPoint) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format64bppBGRAFixedPoint) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format64bppRGBFixedPoint) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format64bppRGBHalf) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format48bppRGBHalf) return PixelFormat.Format64bppRGBAHalf;
            else if (wicFormatGUID == PixelFormat.Format128bppPRGBAFloat) return PixelFormat.Format128bppRGBAFloat;
            else if (wicFormatGUID == PixelFormat.Format128bppRGBFloat) return PixelFormat.Format128bppRGBAFloat;
            else if (wicFormatGUID == PixelFormat.Format128bppRGBAFixedPoint) return PixelFormat.Format128bppRGBAFloat;
            else if (wicFormatGUID == PixelFormat.Format128bppRGBFixedPoint) return PixelFormat.Format128bppRGBAFloat;
            else if (wicFormatGUID == PixelFormat.Format32bppRGBE) return PixelFormat.Format128bppRGBAFloat;
            else if (wicFormatGUID == PixelFormat.Format32bppCMYK) return PixelFormat.Format32bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format64bppCMYK) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format40bppCMYKAlpha) return PixelFormat.Format64bppRGBA;
            else if (wicFormatGUID == PixelFormat.Format80bppCMYKAlpha) return PixelFormat.Format64bppRGBA;
            else return PixelFormat.FormatDontCare;
        }

        // get the dxgi format equivilent of a wic format
        public static Format GetDXGIFormatFromWICFormat(Guid wicFormatGUID)
        {
            if (wicFormatGUID == PixelFormat.Format128bppRGBAFloat) return Format.R32G32B32A32_Float;
            else if (wicFormatGUID == PixelFormat.Format64bppRGBAHalf) return Format.R16G16B16A16_Float;
            else if (wicFormatGUID == PixelFormat.Format64bppRGBA) return Format.R16G16B16A16_UNorm;
            else if (wicFormatGUID == PixelFormat.Format32bppRGBA) return Format.R8G8B8A8_UNorm;
            else if (wicFormatGUID == PixelFormat.Format32bppBGRA) return Format.B8G8R8A8_UNorm;
            else if (wicFormatGUID == PixelFormat.Format32bppBGR) return Format.B8G8R8X8_UNorm;
            else if (wicFormatGUID == PixelFormat.Format32bppRGBA1010102XR) return Format.R10G10B10_Xr_Bias_A2_UNorm;
            else if (wicFormatGUID == PixelFormat.Format32bppRGBA1010102) return Format.R10G10B10A2_UNorm;
            else if (wicFormatGUID == PixelFormat.Format16bppBGRA5551) return Format.B5G5R5A1_UNorm;
            else if (wicFormatGUID == PixelFormat.Format16bppBGR565) return Format.B5G6R5_UNorm;
            else if (wicFormatGUID == PixelFormat.Format32bppGrayFloat) return Format.R32_Float;
            else if (wicFormatGUID == PixelFormat.Format16bppGrayHalf) return Format.R16_Float;
            else if (wicFormatGUID == PixelFormat.Format16bppGray) return Format.R16_UNorm;
            else if (wicFormatGUID == PixelFormat.Format8bppGray) return Format.R8_UNorm;
            else if (wicFormatGUID == PixelFormat.Format8bppAlpha) return Format.A8_UNorm;
            else return Format.Unknown;
        }
    }
}