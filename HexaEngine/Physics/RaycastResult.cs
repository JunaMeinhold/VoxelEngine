namespace HexaEngine.Physics
{
    using System.Numerics;
    using Vortice.Mathematics;

    public struct RaycastResult
    {
        public bool Hit { get; set; }
        public Ray Ray { get; set; }

        public Vector3 Position { get; set; }
    }
}