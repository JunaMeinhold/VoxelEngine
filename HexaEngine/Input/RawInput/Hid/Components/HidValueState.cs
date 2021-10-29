namespace HexaEngine.Input.RawInput
{
    using HexaEngine.Input.RawInput.Native;

    public class HidValueState
    {
        private readonly byte[] report;
        private readonly int reportLength;

        public HidValue Value { get; }

        public int CurrentValue
        {
            get
            {
                using HidPreparsedDataPtr preparsedDataPtr = Value.reader.GetPreparsedData();
                return HidP.GetUsageValue(preparsedDataPtr, HidPReportType.Input, Value.valueCaps, Value.UsageAndPage.Usage, report, reportLength);
            }
        }

        public int? ScaledValue
        {
            get
            {
                using HidPreparsedDataPtr preparsedDataPtr = Value.reader.GetPreparsedData();
                return HidP.TryGetScaledUsageValue(preparsedDataPtr, HidPReportType.Input, Value.valueCaps, Value.UsageAndPage.Usage, report, reportLength, out int value) == NtStatus.Success
                    ? value
                    : null;
            }
        }

        public bool HasValue
        {
            get
            {
                if (!Value.CanBeNull) return true;

                int currentValue = CurrentValue;

                return currentValue >= Value.MinValue && currentValue <= Value.MaxValue;
            }
        }

        internal HidValueState(HidValue value, byte[] report, int reportLength)
        {
            Value = value;
            this.report = report;
            this.reportLength = reportLength;
        }

        public override string ToString() =>
            $"Value: {{{Value}}}, CurrentValue: {CurrentValue}";
    }
}