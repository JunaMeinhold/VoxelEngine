namespace HexaEngine.Input.RawInput.Native
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// HIDP_CAPS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HidPCaps
    {
        private readonly ushort Usage;
        private readonly ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private readonly ushort[] reserved;

        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDateIndices;

        public HidUsageAndPage UsageAndPage => new HidUsageAndPage(UsagePage, Usage);
    }
}