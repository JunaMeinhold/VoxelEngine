namespace VoxelEngine.Audio
{
    using Vortice.Multimedia;
    using Vortice.XAudio2;
    using VoxelEngine.IO;

    public class SourceVoice
    {
        private bool disposedValue;

        public IXAudio2SourceVoice Audio2SourceVoice { get; private set; }

        public AudioBuffer Buffer { get; private set; }

        public bool Playing { get; private set; }

        public Dictionary<string, int> Groups { get; } = new();

        public event EventHandler StoppedPlaying;

        public void LoadWav(string path)
        {
            // Open the wave file in binary.
            BinaryReader reader = new(FileSystem.Open(Paths.CurrentSoundPath + path));

            // Read in the wave file header.
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();
            var format = new string(reader.ReadChars(4));
            var subChunkId = new string(reader.ReadChars(4));
            var subChunkSize = reader.ReadInt32();
            var audioFormat = (WaveFormatEncoding)reader.ReadInt16();
            var numChannels = reader.ReadInt16();
            var sampleRate = reader.ReadInt32();
            var bytesPerSecond = reader.ReadInt32();
            var blockAlign = reader.ReadInt16();
            var bitsPerSample = reader.ReadInt16();
            var dataChunkId = new string(reader.ReadChars(4));
            var dataSize = reader.ReadInt32();

            // Check that the chunk ID is the RIFF format
            // and the file format is the WAVE format
            // and sub chunk ID is the fmt format
            // and the audio format is PCM
            // and the wave file was recorded in stereo format
            // and at a sample rate of 44.1 KHz
            // and at 16 bit format
            // and there is the data chunk header.
            // Otherwise return false.
            // modified in Tutorial 31 for 3D Sound loading stereo files in a mono Secondary buffer.
            if (chunkId != "RIFF" || format != "WAVE" || subChunkId.Trim() != "fmt" || audioFormat != WaveFormatEncoding.Pcm)
            {
                return;
            }

            // prevent other chunkids to be loaded.
            while (dataChunkId != "data")
            {
                reader.BaseStream.Position += dataSize;
                dataChunkId = new string(reader.ReadChars(4));
                dataSize = reader.ReadInt32();
            }

            // Read in the wave file data into the temporary buffer.
            byte[] waveData = reader.ReadBytes(dataSize);

            // Close the reader
            reader.Close();

            Buffer = new(waveData, BufferFlags.EndOfStream);
            WaveFormat waveFormat = new(sampleRate, bitsPerSample, numChannels);
            Audio2SourceVoice = AudioManager.IXAudio2.CreateSourceVoice(waveFormat, flags: VoiceFlags.UseFilter);
            Audio2SourceVoice.SubmitSourceBuffer(Buffer);
            Audio2SourceVoice.StreamEnd += () => StoppedPlaying?.Invoke(this, null);
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
            Audio2SourceVoice.SetOutputVoices(AudioManager.GetVoiceSendDescriptors(Groups.ToList().ConvertAll(x => x.Key)).ToArray());
        }

        internal void Play()
        {
            Playing = true;
            Audio2SourceVoice.SubmitSourceBuffer(Buffer);
            Audio2SourceVoice.Start(0);
        }

        internal void Play(int volume)
        {
            Playing = true;
            Audio2SourceVoice.SubmitSourceBuffer(Buffer);
            Audio2SourceVoice.SetVolume(volume);
            Audio2SourceVoice.Start(0);
        }

        internal void Stop()
        {
            Audio2SourceVoice.Stop(0);
            Playing = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Stop();
                Audio2SourceVoice.Dispose();
                Audio2SourceVoice = null;
                Buffer.Dispose();
                Buffer = null;
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