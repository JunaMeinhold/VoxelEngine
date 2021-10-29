namespace HexaEngine.Input.RawInput
{
    using System;
    using HexaEngine.Input.RawInput.Native;

    public class HidButtonState
    {
        private readonly byte[] report;
        private readonly int reportLength;

        public HidButton Button { get; }

        public bool IsActive
        {
            get
            {
                using HidPreparsedDataPtr preparsedDataPtr = Button.reader.GetPreparsedData();
                ushort[] activeUsages = HidP.GetUsages(preparsedDataPtr, HidPReportType.Input, Button.buttonCaps, report, reportLength);

                return Array.IndexOf(activeUsages, Button.UsageAndPage.Usage) != -1;
            }
        }

        internal HidButtonState(HidButton button, byte[] report, int reportLength)
        {
            Button = button;
            this.report = report;
            this.reportLength = reportLength;
        }

        public override string ToString() =>
            $"Button: {{{Button}}}, IsActive: {IsActive}";
    }
}