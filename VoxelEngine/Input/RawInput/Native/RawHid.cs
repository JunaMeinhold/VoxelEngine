namespace HexaEngine.Input.RawInput.Native
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;

    /// <summary>
    /// RAWHID
    /// </summary>
    public struct RawHid
    {
        private int dwSizeHid;
        private int dwCount;
        private byte[] rawData;

        public int ElementSize => dwSizeHid;
        public int Count => dwCount;
        public unsafe byte[] RawData => rawData;

        public static unsafe RawHid FromPointer(void* ptr)
        {
            RawHid result = new RawHid();
            int* intPtr = (int*)ptr;

            result.dwSizeHid = intPtr[0];
            result.dwCount = intPtr[1];
            result.rawData = new byte[result.ElementSize * result.Count];
            Marshal.Copy(new IntPtr(&intPtr[2]), result.rawData, 0, result.rawData.Length);

            return result;
        }

        public ArraySegment<byte>[] ToHidReports()
        {
            int elementSize = ElementSize;
            byte[] rawDataArray = RawData;

            return Enumerable.Range(0, Count)
                             .Select(x => new ArraySegment<byte>(rawDataArray, elementSize * x, elementSize))
                             .ToArray();
        }

        public unsafe byte[] ToStructure()
        {
            byte[] result = new byte[dwSizeHid * dwCount + sizeof(int) * 2];

            fixed (byte* resultPtr = result)
            {
                int* intPtr = (int*)resultPtr;

                intPtr[0] = dwSizeHid;
                intPtr[1] = dwCount;
            }

            rawData.CopyTo(result, sizeof(int) * 2);

            return result;
        }

        public override string ToString() =>
            $"{{Count: {Count}, Size: {ElementSize}, Content: {BitConverter.ToString(RawData).Replace("-", " ")}}}";
    }
}