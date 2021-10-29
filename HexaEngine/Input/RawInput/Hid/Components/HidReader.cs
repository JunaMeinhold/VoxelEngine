namespace HexaEngine.Input.RawInput
{
    using System.Linq;
    using HexaEngine.Input.RawInput.Native;

    public class HidReader
    {
        private HidPCaps capabilities;

        public byte[] PreparsedData
        {
            get;
        }

        public HidPreparsedData PreparsedDataPtr
        {
            get;
        }

        public int ValueCount => capabilities.NumberInputValueCaps;
        public HidButtonSet[] ButtonSets { get; private set; }
        public HidValueSet[] ValueSets { get; private set; }

        public HidReader(HidPreparsedData preparsedData) =>
            Initialize(PreparsedDataPtr = preparsedData);

        public HidReader(byte[] preparsedData)
        {
            using HidPreparsedDataPtr preparsedDataPtr = new HidPreparsedDataPtr(PreparsedData = preparsedData);
            Initialize(preparsedDataPtr);
        }

        private void Initialize(HidPreparsedData preparsedData)
        {
            capabilities = HidP.GetCaps(preparsedData);

            HidPButtonCaps[] buttonCaps = HidP.GetButtonCaps(preparsedData, HidPReportType.Input);

            ButtonSets = buttonCaps.Select(i => new HidButtonSet(this, i)).ToArray();

            HidPValueCaps[] valueCaps = HidP.GetValueCaps(preparsedData, HidPReportType.Input);

            ValueSets = valueCaps.Select(i => new HidValueSet(this, i)).ToArray();
        }

        internal HidPreparsedDataPtr GetPreparsedData() =>
            PreparsedData == null
                ? new HidPreparsedDataPtr(PreparsedDataPtr)
                : new HidPreparsedDataPtr(PreparsedData);
    }
}