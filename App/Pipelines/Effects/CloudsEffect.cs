namespace App.Pipelines.Effects
{
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Rendering.D3D;

    public class CloudsEffect
    {
        private ConstantBuffer<CBWeather> constantBuffer;

        public CloudsEffect(ID3D11Device device)
        {
            TextureHelper.LoadFromFile("");
        }
    }
}