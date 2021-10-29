namespace HexaEngine.Input.RawInput
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class HidValueSetState : IEnumerable<HidValueState>
    {
        private readonly byte[] report;
        private readonly int reportLength;

        public HidValueSet ValueSet { get; }

        public int[] CurrentValues => this.Select(x => x.CurrentValue).ToArray();

        public int?[] ScaledValues => this.Select(x => x.ScaledValue).ToArray();

        internal HidValueSetState(HidValueSet valueSet, byte[] report, int reportLength)
        {
            ValueSet = valueSet;
            this.report = report;
            this.reportLength = reportLength;
        }

        public override string ToString() =>
            $"ValueSet: {{{ValueSet}}}, CurrentValues: [{string.Join(", ", CurrentValues)}]";

        public IEnumerator<HidValueState> GetEnumerator()
        {
            for (ushort usage = ValueSet.UsageMin; usage <= ValueSet.UsageMax; usage++)
                yield return new HidValueState(new HidValue(ValueSet.reader, ValueSet.valueCaps, usage), report, reportLength);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}