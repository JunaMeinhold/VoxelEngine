namespace HexaEngine.Physics
{
    using System.Numerics;
    using Vortice.Mathematics;

    public class Actor
    {
        public Vector3 Position { get; set; }

        public Vector3 Accelleration { get; set; }

        public Vector3 Velocity { get; set; }

        public Vector3 Force { get; set; }

        public BoundingBox BoundingBox { get; set; }
    }
}