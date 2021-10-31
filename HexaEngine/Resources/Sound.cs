using HexaEngine.Audio;
using HexaEngine.IO;
using System;
using System.IO;
using System.Numerics;
using Vortice.Multimedia;
using Vortice.XAudio2;

namespace HexaEngine.Resources
{
    public class Sound : Resource
    {
        public IXAudio2SourceVoice SourceVoice { get; private set; }

        public AudioBuffer Buffer { get; private set; }

        public AudioManager Manager { get; private set; }

        public Emitter Emitter { get; set; }

        public bool Playing { get; private set; }

        // Constructor
        public Sound()
        {
        }

        public static Sound Load(string path)
        {
            return ResourceManager.LoadSound(path);
        }

        // Virtual Methods
        internal void LoadAudioFile(AudioManager manager, string audioFile)
        {
            Manager = manager;

            // Open the wave file in binary.
            BinaryReader reader = new(FileSystem.Open(audioFile));

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
                return;

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

            var waveFormat = new WaveFormat(sampleRate, bitsPerSample, numChannels);

            SourceVoice = Manager.IXAudio2.CreateSourceVoice(waveFormat, flags: VoiceFlags.UseFilter);
            Buffer = new(waveData, BufferFlags.EndOfStream);
            SourceVoice.SubmitSourceBuffer(Buffer);
            SourceVoice.StreamEnd += SourceVoice_StreamEnd;
        }

        private void SourceVoice_StreamEnd()
        {
            Playing = false;
        }

        public void Tick()
        {
            Manager.Update(SourceVoice, Emitter);
        }

        public void Play(int volume)
        {
            Playing = true;
            SourceVoice.SubmitSourceBuffer(Buffer);
            SourceVoice.SetVolume(volume);
            SourceVoice.Start(0);
        }

        public void Play()
        {
            Playing = true;
            SourceVoice.SubmitSourceBuffer(Buffer);
            Tick();
            SourceVoice.Start(0);
        }
    }
}