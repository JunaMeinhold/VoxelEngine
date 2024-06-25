namespace VoxelEngine.Rendering.D3D
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Core;
    using VoxelEngine.Rendering.D3D.Interfaces;
    using VoxelEngine.Rendering.D3D.Shaders;
    using VoxelEngine.Resources;

    public class RenderTextureArray : Resource, IShaderResource
    {
        private readonly List<ShaderResourceBinding> bindings = new();
        private ID3D11Texture2D[] textures;
        private ID3D11ShaderResourceView[] resourceViews;

        public int Width { get; }

        public int Height { get; }

        public int Count { get; }

        public RenderTargetArray RenderTargets { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderTextureArray(ID3D11Device device, int width, int height, int count = 1, Format format = Format.R32G32B32A32_Float)
        {
            Count = count;
            Width = width;
            Height = height;
            textures = new ID3D11Texture2D[count];
            resourceViews = new ID3D11ShaderResourceView[count];
            for (int i = 0; i < count; i++)
            {
                ID3D11Texture2D texture;
                ID3D11ShaderResourceView resourceView;
                if (Nucleus.Settings.MSAA)
                {
                    Texture2DDescription textureDesc = new()
                    {
                        Width = Width,
                        Height = Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = format,
                        SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                        CPUAccessFlags = CpuAccessFlags.None,
                        MiscFlags = ResourceOptionFlags.None
                    };

                    texture = device.CreateTexture2D(textureDesc);

                    ShaderResourceViewDescription srvDesc = new()
                    {
                        Format = texture.Description.Format,
                        ViewDimension = ShaderResourceViewDimension.Texture2DMultisampled,
                    };

                    resourceView = device.CreateShaderResourceView(texture, srvDesc);
                }
                else
                {
                    Texture2DDescription textureDesc = new()
                    {
                        Width = Width,
                        Height = Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = format,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                        CPUAccessFlags = CpuAccessFlags.None,
                        MiscFlags = ResourceOptionFlags.None
                    };

                    texture = device.CreateTexture2D(textureDesc);

                    ShaderResourceViewDescription srvDesc = new()
                    {
                        Format = texture.Description.Format,
                        ViewDimension = ShaderResourceViewDimension.Texture2D,
                    };

                    srvDesc.Texture2D.MipLevels = 1;
                    srvDesc.Texture2D.MostDetailedMip = 0;

                    resourceView = device.CreateShaderResourceView(texture, srvDesc);
                }
                textures[i] = texture;
                resourceViews[i] = resourceView;
            }

            RenderTargets = new(device, textures, width, height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderTextureArray(ID3D11Device device, int width, int height, params Format[] formats)
        {
            Count = formats.Length;
            Width = width;
            Height = height;
            textures = new ID3D11Texture2D[formats.Length];
            resourceViews = new ID3D11ShaderResourceView[formats.Length];
            for (int i = 0; i < formats.Length; i++)
            {
                ID3D11Texture2D texture;
                ID3D11ShaderResourceView resourceView;
                if (Nucleus.Settings.MSAA)
                {
                    Texture2DDescription textureDesc = new()
                    {
                        Width = Width,
                        Height = Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = formats[i],
                        SampleDescription = new SampleDescription(Nucleus.Settings.MSAASampleCount, Nucleus.Settings.MSAASampleQuality),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                        CPUAccessFlags = CpuAccessFlags.None,
                        MiscFlags = ResourceOptionFlags.None
                    };

                    texture = device.CreateTexture2D(textureDesc);

                    ShaderResourceViewDescription srvDesc = new()
                    {
                        Format = texture.Description.Format,
                        ViewDimension = ShaderResourceViewDimension.Texture2DMultisampled,
                    };

                    resourceView = device.CreateShaderResourceView(texture, srvDesc);
                }
                else
                {
                    Texture2DDescription textureDesc = new()
                    {
                        Width = Width,
                        Height = Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = formats[i],
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                        CPUAccessFlags = CpuAccessFlags.None,
                        MiscFlags = ResourceOptionFlags.None
                    };

                    texture = device.CreateTexture2D(textureDesc);

                    ShaderResourceViewDescription srvDesc = new()
                    {
                        Format = texture.Description.Format,
                        ViewDimension = ShaderResourceViewDimension.Texture2D,
                    };

                    srvDesc.Texture2D.MipLevels = 1;
                    srvDesc.Texture2D.MostDetailedMip = 0;

                    resourceView = device.CreateShaderResourceView(texture, srvDesc);
                }
                textures[i] = texture;
                resourceViews[i] = resourceView;
            }

            RenderTargets = new(device, textures, width, height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ShaderResourceBinding binding)
        {
            bindings.Add(binding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(ID3D11DeviceContext context)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                ShaderResourceBinding binding = bindings[i];
                switch (binding.Stage)
                {
                    case ShaderStage.Vertex:
                        context.VSSetShaderResources(binding.Slot, resourceViews);
                        break;

                    case ShaderStage.Hull:
                        context.HSSetShaderResources(binding.Slot, resourceViews);
                        break;

                    case ShaderStage.Domain:
                        context.DSSetShaderResources(binding.Slot, resourceViews);
                        break;

                    case ShaderStage.Pixel:
                        context.PSSetShaderResources(binding.Slot, resourceViews);
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ShaderResourceBinding binding)
        {
            bindings.Remove(binding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unbind(ID3D11DeviceContext context)
        {
            foreach (ShaderResourceBinding binding in bindings)
            {
                switch (binding.Stage)
                {
                    case ShaderStage.Vertex:
                        context.VSSetShaderResources(binding.Slot, new ID3D11ShaderResourceView[resourceViews.Length]);
                        break;

                    case ShaderStage.Hull:
                        context.HSSetShaderResources(binding.Slot, new ID3D11ShaderResourceView[resourceViews.Length]);
                        break;

                    case ShaderStage.Domain:
                        context.DSSetShaderResources(binding.Slot, new ID3D11ShaderResourceView[resourceViews.Length]);
                        break;

                    case ShaderStage.Pixel:
                        context.PSSetShaderResources(binding.Slot, new ID3D11ShaderResourceView[resourceViews.Length]);
                        break;
                }
            }
        }

        public static implicit operator ID3D11ShaderResourceView[](RenderTextureArray array)
        {
            return array.resourceViews;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (ID3D11Texture2D texture in textures)
            {
                texture.Dispose();
            }

            foreach (ID3D11ShaderResourceView view in resourceViews)
            {
                view.Dispose();
            }

            textures = null;
            resourceViews = null;
            RenderTargets.Dispose();
        }
    }
}