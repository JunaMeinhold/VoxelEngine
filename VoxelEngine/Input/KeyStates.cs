using System;

namespace HexaEngine.Input
{
    [Flags]
    public enum KeyStates
    {
        Released = 0,
        Pressed = -127,
        Toggled = 1
    }
}