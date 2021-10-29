namespace HexaEngine.Objects
{
    using HexaEngine.Input;
    using HexaEngine.Scenes;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Scenes.Objects;
    using HexaEngine.Windows;
    using System.Numerics;
    using VoxelGen;

    public class Player : IFrameScriptObject
    {
        private const float DegToRadFactor = 0.0174532925f;
        public float Speed = 20F;
        public float AngluarSpeed = 20F;
        private bool leftDown;

        public Player(World world)
        {
            Camera = new Camera()
            {
                Fov = 120,
                Type = CameraType.Perspective,
                FarPlane = 1000f,
                PositionZ = -2,
            };
            World = world;
        }

        public IObjectRenderer Renderer { get; } = null;

        public Matrix4x4 Transform { get; }

        public Camera Camera { get; }

        public World World { get; }

        public void Awake()
        {
        }

        public void Initialize()
        {
        }

        public void Sleep()
        {
        }

        public void Uninitialize()
        {
        }

        public void Update()
        {
            var delta = Mouse.GetDelta();
            if (delta.X != 0)
            {
                Camera.AdjustRotation(new Vector3(0, delta.X * AngluarSpeed * Time.Delta, 0));
            }
            if (delta.Y != 0)
            {
                Camera.AdjustRotation(new Vector3(delta.Y * AngluarSpeed * Time.Delta, 0, 0));
                if (Camera.RotationX < 270 & Camera.RotationX > 180)
                {
                    Camera.RotationX = 270;
                }
                if (Camera.RotationX > 90 & Camera.RotationX < 270)
                {
                    Camera.RotationX = 90;
                }
            }

            if (Keyboard.IsDown(Keys.W))
            {
                var rotation = Matrix4x4.CreateFromYawPitchRoll(Camera.RotationY * DegToRadFactor, Camera.RotationX * DegToRadFactor, 0f);
                if (Keyboard.IsDown(Keys.Shift))
                    Camera.AdjustPosition(Vector3.Transform(Vector3.UnitZ, rotation) * Speed * 2 * Time.Delta);
                else
                    Camera.AdjustPosition(Vector3.Transform(Vector3.UnitZ, rotation) * Speed * Time.Delta);
            }
            if (Keyboard.IsDown(Keys.S))
            {
                var rotation = Matrix4x4.CreateFromYawPitchRoll(Camera.RotationY * DegToRadFactor, 0, 0f);
                if (Keyboard.IsDown(Keys.Shift))
                    Camera.AdjustPosition(Vector3.Transform(-Vector3.UnitZ, rotation) * Speed * 2 * Time.Delta);
                else
                    Camera.AdjustPosition(Vector3.Transform(-Vector3.UnitZ, rotation) * Speed * Time.Delta);
            }
            if (Keyboard.IsDown(Keys.A))
            {
                var rotation = Matrix4x4.CreateFromYawPitchRoll(Camera.RotationY * DegToRadFactor, 0, 0f);
                if (Keyboard.IsDown(Keys.Shift))
                    Camera.AdjustPosition(Vector3.Transform(-Vector3.UnitX, rotation) * Speed * 2 * Time.Delta);
                else
                    Camera.AdjustPosition(Vector3.Transform(-Vector3.UnitX, rotation) * Speed * Time.Delta);
            }
            if (Keyboard.IsDown(Keys.D))
            {
                var rotation = Matrix4x4.CreateFromYawPitchRoll(Camera.RotationY * DegToRadFactor, 0, 0f);
                if (Keyboard.IsDown(Keys.Shift))
                    Camera.AdjustPosition(Vector3.Transform(Vector3.UnitX, rotation) * Speed * 2 * Time.Delta);
                else
                    Camera.AdjustPosition(Vector3.Transform(Vector3.UnitX, rotation) * Speed * Time.Delta);
            }
            if (Keyboard.IsDown(Keys.Space))
            {
                var rotation = Matrix4x4.CreateFromYawPitchRoll(Camera.RotationY * DegToRadFactor, 0, 0f);
                if (Keyboard.IsDown(Keys.Shift))
                    Camera.AdjustPosition(Vector3.Transform(Vector3.UnitY, rotation) * Speed * 2 * Time.Delta);
                else
                    Camera.AdjustPosition(Vector3.Transform(Vector3.UnitY, rotation) * Speed * Time.Delta);
            }
            if (Keyboard.IsDown(Keys.C))
            {
                var rotation = Matrix4x4.CreateFromYawPitchRoll(Camera.RotationY * DegToRadFactor, 0, 0f);
                if (Keyboard.IsDown(Keys.Shift))
                    Camera.AdjustPosition(Vector3.Transform(-Vector3.UnitY, rotation) * Speed * 2 * Time.Delta);
                else
                    Camera.AdjustPosition(Vector3.Transform(-Vector3.UnitY, rotation) * Speed * Time.Delta);
            }
            if (!Mouse.IsDown(MouseButton.LButton) & leftDown)
            {
                leftDown = false;
            }

            if (Mouse.IsDown(MouseButton.LButton) & !leftDown)
            {
                leftDown = true;
                World.Raycast(x =>
                {
                    if (x.Hit)
                    {
                        World.SetBlock((int)x.Position.X, (int)x.Position.Y, (int)x.Position.Z, default);
                    }
                    return true;
                }, new(Camera.Position, Camera.Forward * 5), 5f);
            }
        }

        public void UpdateFixed()
        {
        }
    }
}