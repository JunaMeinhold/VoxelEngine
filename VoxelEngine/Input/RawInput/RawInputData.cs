namespace HexaEngine.Input.RawInput
{
    using System;
    using System.Linq;
    using HexaEngine.Input.RawInput.Native;

    public abstract class RawInputData
    {
        private RawInputDevice device;

        public RawInputHeader Header { get; }

        public RawInputDevice Device =>
            device ??= Header.DeviceHandle != RawInputDeviceHandle.Zero
                ? RawInputDevice.FromHandle(Header.DeviceHandle)
                : null;

        protected RawInputData(RawInputHeader header)
        {
            Header = header;
        }

        public static RawInputData FromHandle(IntPtr lParam)
            => FromHandle((RawInputHandle)lParam);

        public static RawInputData FromHandle(RawInputHandle rawInput)
        {
            RawInputHeader header = User32.GetRawInputDataHeader(rawInput);

            return header.Type switch
            {
                RawInputDeviceType.Mouse => new RawInputMouseData(header, User32.GetRawInputMouseData(rawInput, out _)),
                RawInputDeviceType.Keyboard => new RawInputKeyboardData(header, User32.GetRawInputKeyboardData(rawInput, out _)),
                RawInputDeviceType.Hid => RawInputHidData.Create(header, User32.GetRawInputHidData(rawInput, out _)),
                _ => throw new ArgumentException(),
            };
        }

        private static unsafe RawInputData ParseRawInputBufferItem(byte* ptr)
        {
            RawInputHeader header = *(RawInputHeader*)ptr;
            int headerSize = MarshalEx.SizeOf<RawInputHeader>();
            byte* dataPtr = ptr + headerSize;

            // RAWINPUT structure must be aligned by 8 bytes on WOW64
            // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputbuffer#remarks
            if (EnvironmentEx.Is64BitProcess && EnvironmentEx.Is64BitOperatingSystem) dataPtr += 8;

            return header.Type switch
            {
                RawInputDeviceType.Mouse => new RawInputMouseData(header, *(RawMouse*)dataPtr),
                RawInputDeviceType.Keyboard => new RawInputKeyboardData(header, *(RawKeyboard*)dataPtr),
                RawInputDeviceType.Hid => RawInputHidData.Create(header, RawHid.FromPointer(dataPtr)),
                _ => throw new ArgumentException(),
            };
        }

        public static unsafe RawInputData[] GetBufferedData(int bufferSize = 8)
        {
            uint itemSize = User32.GetRawInputBufferSize();
            if (itemSize == 0) return Array.Empty<RawInputData>();

            byte[] bytes = new byte[itemSize * bufferSize];

            fixed (byte* bytesPtr = bytes)
            {
                uint count = User32.GetRawInputBuffer((IntPtr)bytesPtr, (uint)bytes.Length);
                if (count == 0) return Array.Empty<RawInputData>();

                RawInputData[] result = new RawInputData[count];

                for (int i = 0, offset = 0; i < result.Length; i++)
                {
                    RawInputData data = ParseRawInputBufferItem(bytesPtr + offset);

                    result[i] = data;
                    offset = Align(offset + data.Header.Size);
                }

                return result;
            }
        }

        protected static int Align(int x) => (x + IntPtr.Size - 1) & ~(IntPtr.Size - 1);

        public static void DefRawInputProc(RawInputData[] data) =>
            User32.DefRawInputProc(data.SelectMany(i => i.ToStructure()).ToArray());

        public abstract byte[] ToStructure();
    }
}