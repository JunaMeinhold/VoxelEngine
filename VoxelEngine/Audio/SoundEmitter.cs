namespace VoxelEngine.Audio
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Hexa.NET.X3DAudio;
    using Hexa.NET.XAudio2;

    public unsafe class SoundEmitter
    {
        private bool disposedValue;
        private X3DAudioEmitter emitter;

        public SoundEmitter()
        {
            emitter = new();
            emitter.CurveDistanceScaler = 1;
        }

        public X3DAudioEmitter Emitter { get => emitter; set => emitter = value; }

        public List<SourceVoice> PlayingVoices { get; set; } = new();

        public string Group { get; set; } = "Master";

        public Vector3 OrientFront { get => emitter.OrientFront; set => emitter.OrientFront = value; }

        public Vector3 OrientTop { get => emitter.OrientTop; set => emitter.OrientTop = value; }

        public Vector3 Position { get => emitter.Position; set => emitter.Position = value; }

        public Vector3 Velocity { get => emitter.Velocity; set => emitter.Velocity = value; }

        public float InnerRadius { get => emitter.InnerRadius; set => emitter.InnerRadius = value; }

        public float InnerRadiusAngle { get => emitter.InnerRadiusAngle; set => emitter.InnerRadiusAngle = value; }

        public uint ChannelCount { get => emitter.ChannelCount; set => emitter.ChannelCount = value; }

        public float ChannelRadius { get => emitter.ChannelRadius; set => emitter.ChannelRadius = value; }

        public float CurveDistanceScaler { get => Emitter.CurveDistanceScaler; set => emitter.CurveDistanceScaler = value; }

        public float DopplerScaler { get => emitter.DopplerScaler; set => emitter.DopplerScaler = value; }

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
                    X3DAudioDspSettings settings = new();
                    settings.SrcChannelCount = svoice.VoiceDetails.InputChannels;
                    settings.DstChannelCount = group.VoiceDetails.InputChannels;
                    settings.PMatrixCoefficients = AllocT<float>(svoice.VoiceDetails.InputChannels * group.VoiceDetails.InputChannels);

                    var listener = SoundListener.Active.Listener;
                    var emitter = Emitter;

                    X3DAudio.X3DAudioCalculate(AudioManager.X3DAudioHandle, &listener, &emitter, X3DAudio.X3DAudio_CALCULATE_MATRIX | X3DAudio.X3DAudio_CALCULATE_DOPPLER | X3DAudio.X3DAudio_CALCULATE_LPF_DIRECT | X3DAudio.X3DAudio_CALCULATE_REVERB, &settings);
                    voice.SetOutputMatrix(group.Voice, svoice.VoiceDetails.InputChannels, group.VoiceDetails.InputChannels, settings.PMatrixCoefficients, 0);
                    voice.SetFrequencyRatio(settings.DopplerFactor, 0);

                    XAudio2FilterParameters parameters = new(XAudio2FilterType.LowPassFilter, 2.0f * MathF.Sin(float.Pi / 6.0f * settings.LPFDirectCoefficient), 1.0f);
                    voice.SetFilterParameters(&parameters, 0);

                    Free(settings.PMatrixCoefficients);
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