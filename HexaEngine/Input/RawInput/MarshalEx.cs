namespace HexaEngine.Input.RawInput
{
    using System.Runtime.InteropServices;

    internal static class MarshalEx
    {
        public static int SizeOf<T>() => Marshal.SizeOf(typeof(T));
    }
}