namespace VoxelEngine.Audio
{
    using Hexa.NET.XAudio2;
    using HexaGen.Runtime.COM;

    public unsafe class MasteringVoice : VoiceGroup
    {
        private bool disposedValue;

        public MasteringVoice()
        {
            IXAudio2MasteringVoice* master;
            AudioManager.IXAudio2.CreateMasteringVoice(&master, 0, 0, 0, null, null, AudioStreamCategory.Other);
            Audio2MasteringVoice = master;
            Voice = (IXAudio2Voice*)master;
            Name = "Master";
            AudioManager.VoiceGroups.Add(this);
        }

        public ComPtr<IXAudio2MasteringVoice> Audio2MasteringVoice { get; set; }

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