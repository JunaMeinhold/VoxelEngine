namespace HexaEngine.Input.RawInput.Native
{
    using System;

    /// <summary>
    /// HRAWINPUT
    /// </summary>
    public struct RawInputHandle : IEquatable<RawInputHandle>
    {
        private readonly IntPtr value;

        public static RawInputHandle Zero => (RawInputHandle)IntPtr.Zero;

        private RawInputHandle(IntPtr value) => this.value = value;

        public static IntPtr GetRawValue(RawInputHandle handle) => handle.value;

        public static explicit operator RawInputHandle(IntPtr value) => new RawInputHandle(value);

        public static bool operator ==(RawInputHandle a, RawInputHandle b) => a.Equals(b);

        public static bool operator !=(RawInputHandle a, RawInputHandle b) => !a.Equals(b);

        public bool Equals(RawInputHandle other) => value.Equals(other.value);

        public override bool Equals(object obj) =>
            obj is RawInputHandle other &&
            Equals(other);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();
    }
}