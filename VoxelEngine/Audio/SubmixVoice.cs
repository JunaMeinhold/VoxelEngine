namespace VoxelEngine.Audio
{
    using Vortice.XAudio2;

    public class SubmixVoice : VoiceGroup
    {
        private bool disposedValue;

        public SubmixVoice()
        {
            Voice = Audio2SubmixVoice = AudioManager.IXAudio2.CreateSubmixVoice();
            AudioManager.VoiceGroups.Add(this);
        }

        public IXAudio2SubmixVoice Audio2SubmixVoice { get; set; }

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