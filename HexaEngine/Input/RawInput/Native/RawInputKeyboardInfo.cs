namespace HexaEngine.Input.RawInput.Native
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// RID_DEVICE_INFO_KEYBOARD
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputKeyboardInfo
    {
        private readonly int dwType;
        private readonly int dwSubType;
        private readonly int dwKeyboardMode;
        private readonly int dwNumberOfFunctionKeys;
        private readonly int dwNumberOfIndicators;
        private readonly int dwNumberOfKeysTotal;

        /// <summary>
        /// dwType
        /// </summary>
        public int KeyboardType => dwType;

        /// <summary>
        /// dwSubType
        /// </summary>
        public int KeyboardSubType => dwSubType;

        /// <summary>
        /// dwKeyboardMode
        /// </summary>
        public int KeyboardMode => dwKeyboardMode;

        /// <summary>
        /// dwNumberOfFunctionKeys
        /// </summary>
        public int FunctionKeyCount => dwNumberOfFunctionKeys;

        /// <summary>
        /// dwNumberOfIndicators
        /// </summary>
        public int IndicatorCount => dwNumberOfIndicators;

        /// <summary>
        /// dwNumberOfKeysTotal
        /// </summary>
        public int TotalKeyCount => dwNumberOfKeysTotal;
    }
}