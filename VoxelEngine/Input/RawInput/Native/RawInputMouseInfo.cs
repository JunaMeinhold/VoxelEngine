namespace HexaEngine.Input.RawInput.Native
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// RID_DEVICE_INFO_MOUSE
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputMouseInfo
    {
        private readonly int dwId;
        private readonly int dwNumberOfButtons;
        private readonly int dwSampleRate;

        [MarshalAs(UnmanagedType.Bool)]
        private readonly bool fHasHorizontalWheel;

        /// <summary>
        /// dwId
        /// </summary>
        public int Id => dwId;

        /// <summary>
        /// dwNumberOfButtons
        /// </summary>
        public int ButtonCount => dwNumberOfButtons;

        /// <summary>
        /// dwSampleRate
        /// </summary>
        public int SampleRate => dwSampleRate;

        /// <summary>
        /// fHasHorizontalWheel
        /// </summary>
        public bool HasHorizontalWheel => fHasHorizontalWheel;
    }
}