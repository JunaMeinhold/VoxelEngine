namespace HexaEngine.Input.RawInput.Digitizer
{
    using System.Linq;
    using HexaEngine.Input.RawInput.Native;

    public class RawInputDigitizerData : RawInputHidData
    {
        public RawInputDigitizerContact[] Contacts { get; }

        public RawInputDigitizerData(RawInputHeader header, RawHid hid)
            : base(header, hid)
        {
            RawInputDigitizer digitizer = (RawInputDigitizer)Device;

            ILookup<int, HidButtonState> contactButtonStates = ButtonSetStates.SelectMany(x => x).Where(x => x.Button.LinkUsageAndPage != digitizer.UsageAndPage).ToLookup(x => x.Button.LinkCollection);
            ILookup<int, HidValueState> contactValueStates = ValueSetStates.SelectMany(x => x).Where(x => x.Value.LinkUsageAndPage != digitizer.UsageAndPage).ToLookup(x => x.Value.LinkCollection);
            int contactCount = ValueSetStates.SelectMany(x => x).FirstOrDefault(x => x.Value.LinkUsageAndPage == digitizer.UsageAndPage && x.Value.UsageAndPage == RawInputDigitizer.UsageContactCount)?.CurrentValue ?? 1;

            Contacts = contactButtonStates.Select(buttonStates => new RawInputDigitizerContact(buttonStates, contactValueStates[buttonStates.Key]))
                                          .Take(contactCount)
                                          .ToArray();
        }
    }
}