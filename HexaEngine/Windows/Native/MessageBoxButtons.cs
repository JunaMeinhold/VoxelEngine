namespace HexaEngine.Windows.Native
{
    public enum MessageBoxButtons : uint
    {
        AbortRetryIgnore = 0x00000002,
        CancelTryContinue = 0x00000006,
        Help = 0x00004000,
        Ok = 0x00000000,
        OkCancel = 0x00000001,
        RetryCancel = 0x00000005,
        YesNo = 0x00000004,
        YesNoCancel = 0x00000003,
    }

    public enum MessageBoxIcon : uint
    {
        None = 0,
        Hand = 0x00000010,
        Question = 0x00000020,
        Exclamation = 0x00000030,
        Asterisk = 0x00000040,
        Stop = Hand,
        Error = Hand,
        Warning = Exclamation,
        Information = Asterisk,
    }

    public enum DialogResult : uint
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7,
        Try = 10,
        Continue = 11,
    }
}