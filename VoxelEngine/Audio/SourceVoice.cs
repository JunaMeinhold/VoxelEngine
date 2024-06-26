namespace VoxelEngine.Audio
{
    using Hexa.NET.XAudio2;
    using HexaGen.Runtime.COM;
    using VoxelEngine.IO;

    public unsafe class SourceVoice
    {
        private bool disposedValue;
        private XAudio2WaveAudioStream? stream;

        public ComPtr<IXAudio2SourceVoice> Audio2SourceVoice { get; private set; }

        public XAudio2VoiceDetails VoiceDetails
        {
            get
            {
                XAudio2VoiceDetails details;
                Audio2SourceVoice.GetVoiceDetails(&details);
                return details;
            }
        }

        public bool Playing { get; private set; }

        public Dictionary<string, int> Groups { get; } = new();

        public event EventHandler StoppedPlaying;

        public void LoadWav(string path)
        {
            stream = new(FileSystem.Open(Paths.CurrentSoundPath + path));

            var waveFormat = stream.GetWaveFormat();
            IXAudio2SourceVoice* sourceVoice;
            AudioManager.IXAudio2.CreateSourceVoice(&sourceVoice, &waveFormat, XAudio2.XAudio2_VOICE_USEFILTER, 1, (IXAudio2VoiceCallback*)null, null, null);
            stream.EndOfStream += OnEndOfStream;
        }

        private void OnEndOfStream()
        {
            StoppedPlaying?.Invoke(this, null);
        }

        internal void AddGroup(string group)
        {
            if (Groups.ContainsKey(group))
            {
                Groups[group]++;
            }
            else
            {
                Groups.Add(group, 1);
                Update();
            }
        }

        internal void RemoveGroup(string group)
        {
            Groups[group]--;
            if (Groups[group] == 0)
            {
                Groups.Remove(group);
                Update();
            }
        }

        internal void Update()
        {
            stream?.Update(Audio2SourceVoice);
            //Audio2SourceVoice.SetOutputVoices(AudioManager.GetVoiceSendDescriptors(Groups.ToList().ConvertAll(x => x.Key)).ToArray());
        }

        internal void Reset()
        {
            stream?.Reset();
        }

        internal void Play()
        {
            Playing = true;
            Audio2SourceVoice.Start(0, 0);
        }

        internal void Play(int volume)
        {
            Playing = true;
            Audio2SourceVoice.SetVolume(volume, 0);
            Audio2SourceVoice.Start(0, 0);
        }

        internal void Stop()
        {
            Audio2SourceVoice.Stop(0, 0);
            Playing = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Stop();
                if (stream != null)
                {
                    stream.EndOfStream -= OnEndOfStream;
                    stream.Dispose();
                }
                Audio2SourceVoice.Dispose();
                Audio2SourceVoice = null;

                disposedValue = true;
            }
        }

        ~SourceVoice()
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