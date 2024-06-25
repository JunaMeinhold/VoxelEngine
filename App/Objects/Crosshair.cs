namespace App.Objects
{
    using App.Renderers.Forward;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Scenes;

    public class Crosshair : GameObject
    {
        private VertexBuffer<OrthoVertex> vertexBuffer;
        private TextureRenderer textureComponent;

        public Crosshair()
        {
            vertexBuffer = new();
            vertexBuffer.DebugName = "Crosshair";
            textureComponent = new();
            textureComponent.TexturePath = "crosshair.png";
            AddComponent(textureComponent);
        }

        public override void Initialize(ID3D11Device device)
        {
            Load();
            base.Initialize(device);
        }

        public override void Uninitialize()
        {
            base.Uninitialize();
            textureComponent = null;
            vertexBuffer = null;
        }

        private void Load()
        {
            int left = -12;
            int top = -12;
            int right = 12;
            int bottom = 12;

            OrthoVertex[] vertices = new OrthoVertex[]
            {
                new()
                {
                    Position = new(right, bottom, 0),
                    Texture = new(1, 0)
                },
                new()
                {
                    Position = new(left, top, 0),
                    Texture = new(0, 1)
                },
                new()
                {
                    Position = new(left, bottom, 0),
                    Texture = new(0, 0)
                },
                new()
                {
                    Position = new(right, top, 0),
                    Texture = new(1, 1)
                },
                new()
                {
                    Position = new(left, top, 0),
                    Texture = new(0, 1)
                },
                new()
                {
                    Position = new(right, bottom, 0),
                    Texture = new(1, 0)
                }
            };

            vertexBuffer.Append(vertices);
            textureComponent.VertexBuffer = vertexBuffer;
        }
    }
}