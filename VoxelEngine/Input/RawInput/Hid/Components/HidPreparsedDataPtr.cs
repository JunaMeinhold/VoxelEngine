namespace HexaEngine.Input.RawInput
{
    using System;
    using System.Runtime.InteropServices;
    using HexaEngine.Input.RawInput.Native;

    public class HidPreparsedDataPtr : SafeHandle
    {
        private readonly GCHandle? gcHandle;

        public HidPreparsedDataPtr(HidPreparsedData handle)
            : base(IntPtr.Zero, true) =>
            this.handle = HidPreparsedData.GetRawValue(handle);

        public HidPreparsedDataPtr(byte[] preparsedData)
            : base(IntPtr.Zero, true)
        {
            gcHandle = GCHandle.Alloc(preparsedData, GCHandleType.Pinned);
            handle = gcHandle.Value.AddrOfPinnedObject();
        }

        public override bool IsInvalid =>
            handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            gcHandle?.Free();

            return true;
        }

        public static implicit operator HidPreparsedData(HidPreparsedDataPtr ptr) =>
            (HidPreparsedData)ptr.DangerousGetHandle();
    }
}