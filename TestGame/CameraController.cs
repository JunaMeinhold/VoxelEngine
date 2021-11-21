namespace TestGame
{
    using HexaEngine.Input;
    using HexaEngine.Scenes.Objects;
    using HexaEngine.Scripting;
    using HexaEngine.Windows;
    using System.Numerics;

    public class CameraController : HexaElement
    {
        private const float DegToRadFactor = 0.0174532925f;
        public float Speed = 10F;
        public float AngluarSpeed = 20F;
        private bool leftDown;

        public CameraController(Camera camera)
        {
            Camera = camera;
        }

        public Camera Camera { get; set; }

        public override void Update()
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
        }
    }
}