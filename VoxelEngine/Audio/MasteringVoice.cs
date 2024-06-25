namespace VoxelEngine.Audio
{
    using Vortice.XAudio2;

    public class MasteringVoice : VoiceGroup
    {
        private bool disposedValue;

        public MasteringVoice()
        {
            Voice = Audio2MasteringVoice = AudioManager.IXAudio2.CreateMasteringVoice();
            Name = "Master";
            AudioManager.VoiceGroups.Add(this);
        }

        public IXAudio2MasteringVoice Audio2MasteringVoice { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Audio2MasteringVoice.Dispose();
                Audio2MasteringVoice = null;
                disposedValue = true;
            }
        }

        ~MasteringVoice()
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