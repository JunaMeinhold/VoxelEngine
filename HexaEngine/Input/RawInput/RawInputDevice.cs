namespace HexaEngine.Input.RawInput
{
    using System;
    using System.Linq;
    using HexaEngine.Input.RawInput.Digitizer;
    using HexaEngine.Input.RawInput.Native;

    public abstract class RawInputDevice
    {
        private string productName;
        private string manufacturerName;

        protected RawInputDeviceInfo DeviceInfo { get; }

        public RawInputDeviceHandle Handle { get; }
        public RawInputDeviceType DeviceType => DeviceInfo.Type;
        public string DevicePath { get; }

        public string ManufacturerName
        {
            get
            {
                if (manufacturerName == null) GetAttributes();
                return manufacturerName;
            }
        }

        public string ProductName
        {
            get
            {
                if (productName == null) GetAttributes();
                return productName;
            }
        }

        public bool IsConnected
        {
            get => CfgMgr32.TryLocateDevNode(DevicePath, CfgMgr32.LocateDevNodeFlags.Normal, out _) == ConfigReturnValue.Success;
        }

        public abstract HidUsageAndPage UsageAndPage { get; }
        public abstract int VendorId { get; }
        public abstract int ProductId { get; }

        private void GetAttributes()
        {
            if (DevicePath == null) return;
            if (manufacturerName == null || productName == null) GetAttributesFromHidD();
            if (manufacturerName == null || productName == null) GetAttributesFromCfgMgr();
        }

        private void GetAttributesFromHidD()
        {
            if (!HidD.TryOpenDevice(DevicePath, out HidDeviceHandle device)) return;

            try
            {
                manufacturerName ??= HidD.GetManufacturerString(device);
                productName ??= HidD.GetProductString(device);
            }
            finally
            {
                HidD.CloseDevice(device);
            }
        }

        private void GetAttributesFromCfgMgr()
        {
            string path = DevicePath.Substring(4).Replace('#', '\\');
            if (path.Contains("{")) path = path.Substring(0, path.IndexOf('{') - 1);

            DeviceInstanceHandle device = CfgMgr32.LocateDevNode(path, CfgMgr32.LocateDevNodeFlags.Phantom);

            manufacturerName ??= CfgMgr32.GetDevNodePropertyString(device, in DevicePropertyKey.DeviceManufacturer);
            productName ??= CfgMgr32.GetDevNodePropertyString(device, in DevicePropertyKey.DeviceFriendlyName);
            productName ??= CfgMgr32.GetDevNodePropertyString(device, in DevicePropertyKey.Name);
        }

        protected RawInputDevice(RawInputDeviceHandle device, RawInputDeviceInfo deviceInfo)
        {
            Handle = device;
            DevicePath = User32.GetRawInputDeviceName(device);
            DeviceInfo = deviceInfo;
        }

        public static RawInputDevice FromHandle(RawInputDeviceHandle device)
        {
            RawInputDeviceInfo deviceInfo = User32.GetRawInputDeviceInfo(device);

            return deviceInfo.Type switch
            {
                RawInputDeviceType.Mouse => new RawInputMouse(device, deviceInfo),
                RawInputDeviceType.Keyboard => new RawInputKeyboard(device, deviceInfo),
                RawInputDeviceType.Hid => RawInputDigitizer.IsSupported(deviceInfo.Hid.UsageAndPage)
? new RawInputDigitizer(device, deviceInfo)
: new RawInputHid(device, deviceInfo),
                _ => throw new ArgumentException(),
            };
        }

        /// <summary>
        /// Gets available devices that can be handled with Raw Input.
        /// </summary>
        /// <returns>Array of <see cref="RawInputDevice"/>, which contains mouse as a <see cref="RawInputMouse"/>, keyboard as a <see cref="RawInputKeyboard"/>, and any other HIDs as a <see cref="RawInputHid"/>.</returns>
        public static RawInputDevice[] GetDevices()
        {
            RawInputDeviceListItem[] devices = User32.GetRawInputDeviceList();

            return devices.Select(i => FromHandle(i.Device)).ToArray();
        }

        public byte[] GetPreparsedData() =>
            User32.GetRawInputDevicePreparsedData(Handle);

        public static void RegisterDevice(HidUsageAndPage usageAndPage, RawInputDeviceFlags flags, IntPtr hWndTarget) =>
            RegisterDevice(new RawInputDeviceRegistration(usageAndPage, flags, hWndTarget));

        public static void RegisterDevice(params RawInputDeviceRegistration[] devices) =>
            User32.RegisterRawInputDevices(devices);

        public static void UnregisterDevice(HidUsageAndPage usageAndPage) =>
            RegisterDevice(usageAndPage, RawInputDeviceFlags.Remove, IntPtr.Zero);

        public static RawInputDeviceRegistration[] GetRegisteredDevices() =>
            User32.GetRegisteredRawInputDevices();
    }
}