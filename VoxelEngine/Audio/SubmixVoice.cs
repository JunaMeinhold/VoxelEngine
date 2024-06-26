namespace VoxelEngine.Audio
{
    using Hexa.NET.XAudio2;
    using HexaGen.Runtime.COM;

    public unsafe class SubmixVoice : VoiceGroup
    {
        private bool disposedValue;

        public SubmixVoice()
        {
            IXAudio2SubmixVoice* submixVoice;
            AudioManager.IXAudio2.CreateSubmixVoice(&submixVoice, 0, 0, 0, 0, null, null);
            Audio2SubmixVoice = submixVoice;
            Voice = (IXAudio2Voice*)submixVoice;
            AudioManager.VoiceGroups.Add(this);
        }

        public ComPtr<IXAudio2SubmixVoice> Audio2SubmixVoice { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Audio2SubmixVoice.Dispose();
                Audio2SubmixVoice = null;
                disposedValue = true;
            }
        }

        ~SubmixVoice()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}