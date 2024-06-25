namespace VoxelEngine.Audio
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Vortice.XAudio2;

    public class SoundEmitter
    {
        private bool disposedValue;

        public SoundEmitter()
        {
            Emitter = new();
            Emitter.CurveDistanceScaler = 1;
        }

        public Emitter Emitter { get; set; }

        public List<SourceVoice> PlayingVoices { get; set; } = new();

        public string Group { get; set; } = "Master";

        public Vector3 OrientFront { get => Emitter.OrientFront; set => Emitter.OrientFront = value; }

        public Vector3 OrientTop { get => Emitter.OrientTop; set => Emitter.OrientTop = value; }

        public Vector3 Position { get => Emitter.Position; set => Emitter.Position = value; }

        public Vector3 Velocity { get => Emitter.Velocity; set => Emitter.Velocity = value; }

        public float InnerRadius { get => Emitter.InnerRadius; set => Emitter.InnerRadius = value; }

        public float InnerRadiusAngle { get => Emitter.InnerRadiusAngle; set => Emitter.InnerRadiusAngle = value; }

        public int ChannelCount { get => Emitter.ChannelCount; set => Emitter.ChannelCount = value; }

        public float ChannelRadius { get => Emitter.ChannelRadius; set => Emitter.ChannelRadius = value; }

        public float CurveDistanceScaler { get => Emitter.CurveDistanceScaler; set => Emitter.CurveDistanceScaler = value; }

        public float DopplerScaler { get => Emitter.DopplerScaler; set => Emitter.DopplerScaler = value; }

        public void Play(SourceVoice voice)
        {
            var vo = voice;
            vo.AddGroup(Group);
            vo.Play();
            voice.StoppedPlaying += Voice_StoppedPlaying;
            PlayingVoices.Add(voice);
        }

        public void Play(SourceVoice voice, int volume)
        {
            var vo = voice;
            vo.AddGroup(Group);
            vo.Play(volume);
            voice.StoppedPlaying += Voice_StoppedPlaying;
            PlayingVoices.Add(voice);
        }

        public void Stop(SourceVoice voice)
        {
            var vo = voice;
            vo.Stop();
            vo.RemoveGroup(Group);
            PlayingVoices.Remove(voice);
        }

        private void StopInternal(SourceVoice voice)
        {
            var vo = voice;
            vo.Stop();
            vo.RemoveGroup(Group);
        }

        private void Voice_StoppedPlaying(object sender, EventArgs e)
        {
            var voice = sender as SourceVoice;
            var vo = voice;
            vo.RemoveGroup(Group);
            PlayingVoices.Remove(voice);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Update()
        {
            if (SoundListener.Active != null)
            {
                foreach (SourceVoice svoice in PlayingVoices)
                {
                    var vo = svoice;
                    var voice = vo.Audio2SourceVoice;
                    var group = AudioManager.GetVoiceGroup(Group);
                    DspSettings settings = new(voice.VoiceDetails.InputChannels, group.Voice.VoiceDetails.InputChannels);
                    AudioManager.X3DAudio.Calculate(SoundListener.Active.Listener, Emitter, CalculateFlags.Matrix | CalculateFlags.Doppler | CalculateFlags.LpfDirect | CalculateFlags.Reverb, settings);
                    voice.SetOutputMatrix(group.Voice, voice.VoiceDetails.InputChannels, group.Voice.VoiceDetails.InputChannels, settings.MatrixCoefficients);
                    voice.SetFrequencyRatio(settings.DopplerFactor, 0);
                    FilterParameters parameters = new() { Type = FilterType.LowPassFilter, Frequency = 2.0f * MathF.Sin(MathF.PI / 6.0f * settings.LpfDirectCoefficient), OneOverQ = 1.0f };
                    voice.SetFilterParameters(parameters, 0);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (var voice in PlayingVoices)
                {
                    StopInternal(voice);
                }

                PlayingVoices.Clear();
                disposedValue = true;
            }
        }

        ~SoundEmitter()
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