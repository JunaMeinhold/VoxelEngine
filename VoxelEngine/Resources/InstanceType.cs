namespace HexaEngine.Resources
{
    using System.Numerics;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceType
    {
        public Vector4 Position { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is InstanceType instance)
            {
                return instance.Position == Position;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public static bool operator ==(InstanceType left, InstanceType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InstanceType left, InstanceType right)
        {
            return !(left == right);
        }
    }
}