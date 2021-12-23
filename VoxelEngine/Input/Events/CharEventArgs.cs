using System;
using System.Collections;

namespace HexaEngine.Input.Events
{
    public class CharEventArgs
    {
        public CharEventArgs(char @char, long extra)
        {
            Char = @char;
            var bytes = BitConverter.GetBytes(extra);
            BitArray array = new(bytes);
            RepeatCount = (ushort)BitConverter.ToInt16(bytes, 0);
            ScanCode = bytes[2];
            Extended = array[24];
            AltPressed = array[29];
            PreviousKeyState = array[30];
            TransitionState = array[31];
        }

        public char Char { get; }

        public ushort RepeatCount { get; }

        public byte ScanCode { get; }

        /// <summary>
        /// Strg or Alt is pressed.
        /// </summary>
        public bool Extended { get; }

        public bool AltPressed { get; }

        public bool PreviousKeyState { get; }

        public bool TransitionState { get; }

        public bool Handled { get; set; }
    }
}