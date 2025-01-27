namespace App.Scripts
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using App.Objects;
    using BepuPhysics.Collidables;
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using VoxelEngine.Core;
    using VoxelEngine.Core.Input;
    using VoxelEngine.Mathematics;
    using VoxelEngine.Physics;
    using VoxelEngine.Physics.Characters;
    using VoxelEngine.Scenes;
    using VoxelEngine.Scripting;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.Blocks;

    public class PlayerController : ScriptFrameComponent
    {
        public float Speed = 20F;
        public float AngluarSpeed = 20F;
        private bool leftDown;
        private bool rightDown;
        private bool midDown;
        private Camera camera;
        private CPlayer player;
        public CharacterInput character;
        private RayHitHandler rayHitHandler;

        public override void Awake()
        {
            player = Parent as CPlayer;
            player.Respawned += Player_Respawned;
            camera = Scene.Camera;

            character = new(Parent.Scene.CharacterControllers, Parent.Transform.Position, new Capsule(0.25f, 1.5f), 0.1f, 1.25f, 100, 100, 5, 4, MathF.PI * 0.4f);
            rayHitHandler = new(Parent.Scene.Simulation, CollidableMobility.Static);
            Keyboard.KeyUp += Keyboard_OnKeyUp;
        }

        private void Keyboard_OnKeyUp(object sender, VoxelEngine.Core.Input.Events.KeyboardEventArgs e)
        {
            if (e.KeyCode == Key.Escape)
            {
                Application.MainWindow.LockCursor = !Application.MainWindow.LockCursor;
            }

            if (e.KeyCode == Key.F1)
            {
                player.Gamemode = Gamemode.Survival;
            }

            if (e.KeyCode == Key.F2)
            {
                player.Gamemode = Gamemode.Creative;
            }
        }

        private void Player_Respawned(object sender, EventArgs e)
        {
            Parent.Scene.Simulation.Bodies[character.BodyHandle].Pose.Position = player.Spawnpoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void Update()
        {
            if (ImGui.Begin("Blocks"))
            {
                ImGui.Text(player.SelectedBlock.Name);
            }
            ImGui.End();

            CameraTransform transform = camera.Transform;
            if (!Application.MainWindow.LockCursor)
            {
                return;
            }

            HandleFreeCamera();
            Parent.Transform.Position = transform.Position - new Vector3(0, 1f, 0);

            RaycastResult result = rayHitHandler.Raycast(transform.Position, transform.Forward, 10);
            if (result.Hit)
            {
                Vector3 hitLocation = transform.Position + transform.Forward * result.T + transform.Forward * 0.01f;
                player.IsLookingAtBlock = true;
                player.LookAtBlock = new((int)Math.Floor(hitLocation.X), (int)Math.Floor(hitLocation.Y), (int)Math.Floor(hitLocation.Z));
            }
            else
            {
                player.IsLookingAtBlock = false;
            }

            if (!Mouse.IsDown(MouseButton.Left) & leftDown)
            {
                leftDown = false;
            }

            if (!Mouse.IsDown(MouseButton.Right) & rightDown)
            {
                rightDown = false;
            }
            if (!Mouse.IsDown(MouseButton.Middle) & midDown)
            {
                midDown = false;
            }

            if (Mouse.IsDown(MouseButton.Left) & !leftDown)
            {
                leftDown = true;
                if (player.IsLookingAtBlock)
                {
                    Scene.Dispatcher.Invoke(() =>
                    {
                        player.World.SetBlock((int)player.LookAtBlock.X, (int)player.LookAtBlock.Y, (int)player.LookAtBlock.Z, default);
                    });
                }
            }

            if (Mouse.IsDown(MouseButton.Right) & !rightDown)
            {
                rightDown = true;
                if (player.IsLookingAtBlock)
                {
                    Vector3 hitLocation = transform.Position + transform.Forward * result.T + transform.Forward * 0.01f;
                    Vector3? index = CalculateAddIndex(hitLocation, player.LookAtBlock);
                    if (index.HasValue)
                    {
                        player.World.SetBlock((int)index.Value.X, (int)index.Value.Y, (int)index.Value.Z, player.SelectedBlock);
                    }
                }
            }

            if (Mouse.IsDown(MouseButton.Middle) & !midDown)
            {
                midDown = true;
                if (player.IsLookingAtBlock)
                {
                    Scene.Dispatcher.Invoke(() =>
                    {
                        player.SelectedBlockId = player.World.GetBlock((int)player.LookAtBlock.X, (int)player.LookAtBlock.Y, (int)player.LookAtBlock.Z).Type;
                    });
                }
            }

            if (Mouse.DeltaWheel.Y == 1)
            {
                int id = player.SelectedBlockId + 1;
                if (id > BlockRegistry.Blocks.Count)
                {
                    id = 1;
                }

                player.SelectedBlockId = id;
            }
            else if (Mouse.DeltaWheel.Y == -1)
            {
                int id = player.SelectedBlockId - 1;
                if (id == 0)
                {
                    id = BlockRegistry.Blocks.Count;
                }

                player.SelectedBlockId = id;
            }

            Vector2 delta = Mouse.Delta * 0.004f;

            if (delta.X != 0)
            {
                transform.Rotation += new Vector3(delta.X * AngluarSpeed, 0, 0);
            }

            if (delta.Y != 0)
            {
                transform.Rotation += new Vector3(0, delta.Y * AngluarSpeed, 0);
                if (transform.Rotation.Y < 269 & transform.Rotation.Y > 180)
                {
                    transform.Rotation = new Vector3(transform.Rotation.X, 269, transform.Rotation.Z);
                }

                if (transform.Rotation.Y > 89 & transform.Rotation.Y < 269)
                {
                    transform.Rotation = new Vector3(transform.Rotation.X, 89, transform.Rotation.Z);
                }
            }
        }

        private void HandleFreeCamera()
        {
            CameraTransform transform = camera.Transform;
            if (Keyboard.IsDown(Key.W))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    transform.Position += transform.Forward * Speed * 2 * Time.Delta;
                }
                else
                {
                    transform.Position += transform.Forward * Speed * Time.Delta;
                }
            }

            if (Keyboard.IsDown(Key.S))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    transform.Position += transform.Backward * Speed * 2 * Time.Delta;
                }
                else
                {
                    transform.Position += transform.Backward * Speed * Time.Delta;
                }
            }

            if (Keyboard.IsDown(Key.A))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    transform.Position += transform.Left * Speed * 2 * Time.Delta;
                }
                else
                {
                    transform.Position += transform.Left * Speed * Time.Delta;
                }
            }

            if (Keyboard.IsDown(Key.D))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    transform.Position += transform.Right * Speed * 2 * Time.Delta;
                }
                else
                {
                    transform.Position += transform.Right * Speed * Time.Delta;
                }
            }

            if (Keyboard.IsDown(Key.Space))
            {
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    transform.Position += Vector3.UnitY * Speed * 2 * Time.Delta;
                }
                else
                {
                    transform.Position += Vector3.UnitY * Speed * Time.Delta;
                }
            }

            if (Keyboard.IsDown(Key.LShift))
            {
                transform.Position += -Vector3.UnitY * Speed * Time.Delta;
            }
        }

        public override void Destroy()
        {
        }

        private static Vector3? CalculateAddIndex(Vector3 relativeLocation, Vector3 index)
        {
            const float size = 1;
            Vector3 voxelLocation = index * size;
            if (NearlyEqual(relativeLocation.X, voxelLocation.X))
            {
                return new Vector3(index.X - 1, index.Y, index.Z);
            }
            else if (NearlyEqual(relativeLocation.X, voxelLocation.X + size))
            {
                return new Vector3(index.X + 1, index.Y, index.Z);
            }
            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y))
            {
                return new Vector3(index.X, index.Y - 1, index.Z);
            }
            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y + size))
            {
                return new Vector3(index.X, index.Y + 1, index.Z);
            }
            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z))
            {
                return new Vector3(index.X, index.Y, index.Z - 1);
            }
            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z + size))
            {
                return new Vector3(index.X, index.Y, index.Z + 1);
            }
            else
            {
                return null;
            }
        }

        public static bool NearlyEqual(float f1, float f2)
        {
            return Math.Abs(f1 - f2) < 0.01;
        }
    }
}