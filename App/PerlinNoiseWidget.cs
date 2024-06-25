namespace App
{
    using System.Numerics;
    using HexaEngine.ImGuiNET;
    using Vortice.Direct3D11;
    using VoxelEngine.Mathematics.Noise;

    public class PerlinNoiseWidget
    {
        private readonly ID3D11Texture2D texture;
        private readonly ID3D11ShaderResourceView shaderResourceView;

        private int seed;

        private Vector2 scale = new(0.02f);

        private int octaves = 3;
        private float persistence = 0.5f;
        private float amplitude = 1;
        private float redistribution = 5f;

        private bool heatmap = false;

        private const int size = 256;

        public PerlinNoiseWidget(ID3D11Device device)
        {
            Texture2DDescription description = new(Vortice.DXGI.Format.R32G32B32A32_Float, size, size, 1, 1, BindFlags.ShaderResource, ResourceUsage.Dynamic, CpuAccessFlags.Write);
            texture = device.CreateTexture2D(description);
            shaderResourceView = device.CreateShaderResourceView(texture);
        }

        private static float SaturateOctave(float value, int octaves, float persistence, float amplitude)
        {
            float result = 0;

            for (int i = 0; i < octaves; i++)
            {
                result += amplitude;
                amplitude *= persistence;
            }

            return value / result;
        }

        public void Draw(ID3D11DeviceContext context)
        {
            if (!ImGui.Begin("Noise"))
            {
                ImGui.End();
                return;
            }

            ImGui.InputInt("Seed", ref seed);
            ImGui.InputFloat2("Scale", ref scale);

            ImGui.Separator();

            ImGui.InputInt("Octaves", ref octaves);
            ImGui.InputFloat("Persistence", ref persistence);
            ImGui.InputFloat("Amplitude", ref amplitude);
            ImGui.InputFloat("Redistribution", ref redistribution);

            ImGui.Separator();

            ImGui.Checkbox("Heatmap", ref heatmap);

            ImGui.Separator();

            if (ImGui.Button("Apply"))
            {
                PerlinNoise noise = new(seed);
                Vector4[] pixels = new Vector4[size * size];

                for (int i = 0; i < size * size; i++)
                {
                    int x = i % size;
                    int y = i / size;

                    float v = noise.OctavePerlin2D(x * scale.X, y * scale.Y, octaves, persistence, amplitude);
                    v = SaturateOctave(v, octaves, persistence, amplitude);
                    v = MathF.Pow(v, redistribution);

                    if (heatmap)
                    {
                        pixels[i] = Vector4.Lerp(new(0, 0, 1, 1), new(1, 0, 0, 1), v);
                    }
                    else
                    {
                        pixels[i] = new(v, v, v, 1);
                    }
                }

                MappedSubresource mapped = context.Map(texture, 0, MapMode.WriteDiscard);
                pixels.CopyTo(mapped.AsSpan<Vector4>(size * size));
                context.Unmap(texture, 0);
            }

            ImGui.Separator();

            ImGui.Image(shaderResourceView.NativePointer, new(size));

            ImGui.End();
        }

        public void Release()
        {
            texture.Dispose();
            shaderResourceView.Dispose();
        }
    }
}