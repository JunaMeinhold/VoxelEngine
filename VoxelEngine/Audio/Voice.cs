namespace VoxelEngine.Audio
{
    using Hexa.NET.XAudio2;
    using HexaGen.Runtime.COM;

    public unsafe class Voice
    {
        public string Name { get; set; }

        public ComPtr<IXAudio2Voice> Audio2Voice { get; set; }

        public XAudio2VoiceDetails VoiceDetails
        {
            get
            {
                XAudio2VoiceDetails details;
                Audio2Voice.GetVoiceDetails(&details);
                return details;
            }
        }
    }
}