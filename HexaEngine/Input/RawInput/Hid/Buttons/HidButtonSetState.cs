namespace HexaEngine.Input.RawInput
{
    using System.Collections;
    using System.Collections.Generic;
    using HexaEngine.Input.RawInput.Native;

    public class HidButtonSetState : IEnumerable<HidButtonState>
    {
        private readonly byte[] report;
        private readonly int reportLength;

        public HidButtonSet ButtonSet { get; }

        public ushort[] ActiveUsages
        {
            get
            {
                using HidPreparsedDataPtr preparsedDataPtr = ButtonSet.reader.GetPreparsedData();
                return HidP.GetUsages(preparsedDataPtr, HidPReportType.Input, ButtonSet.buttonCaps, report, reportLength);
            }
        }

        internal HidButtonSetState(HidButtonSet buttonSet, byte[] report, int reportLength)
        {
            ButtonSet = buttonSet;
            this.report = report;
            this.reportLength = reportLength;
        }

        public override string ToString() =>
            $"ButtonSet: {{{ButtonSet}}}, Active: [{string.Join(", ", ActiveUsages)}]";

        public IEnumerator<HidButtonState> GetEnumerator()
        {
            for (ushort usage = ButtonSet.UsageMin; usage <= ButtonSet.UsageMax; usage++)
                yield return new HidButtonState(new HidButton(ButtonSet.reader, ButtonSet.buttonCaps, usage), report, reportLength);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}