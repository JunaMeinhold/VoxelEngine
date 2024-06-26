namespace VoxelEngine.Audio
{
    using Hexa.NET.XAudio2;
    using HexaGen.Runtime.COM;

    public unsafe class VoiceGroup
    {
        public string Name { get; set; }

        public ComPtr<IXAudio2Voice> Voice { get; set; }

        public XAudio2VoiceDetails VoiceDetails
        {
            get
            {
                XAudio2VoiceDetails details;
                Voice.GetVoiceDetails(&details);
                return details;
            }
        }
    }
}