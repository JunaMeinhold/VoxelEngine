using HexaEngine.Resources;
using HexaEngine.Windows;
using System;
using System.Collections.Generic;
using Vortice.Multimedia;
using Vortice.XAudio2;

namespace HexaEngine.Audio
{
    /// <summary>
    /// Basics sind implementiert aber mehr auch nicht.
    /// </summary>
    public class AudioManager : Disposable
    {
        internal List<Sound> PlayingSounds { get; } = new();

        public X3DAudio X3DAudio { get; private set; }

        public IXAudio2 IXAudio2 { get; private set; }

        public Listener Listener { get; private set; } = new Listener();

        public IXAudio2MasteringVoice MasteringVoice { get; private set; }

        public IXAudio2SubmixVoice SubmixVoice { get; private set; }

        public AudioManager()
        {
            IXAudio2 = XAudio2.XAudio2Create(ProcessorSpecifier.UseDefaultProcessor);
            IXAudio2.StartEngine();
            MasteringVoice = IXAudio2.CreateMasteringVoice();
            SubmixVoice = IXAudio2.CreateSubmixVoice();
            X3DAudio = new((Speakers)MasteringVoice.ChannelMask);
        }

        public void Update(IXAudio2SourceVoice voice, Emitter emitter)
        {
            DspSettings settings = new(voice.VoiceDetails.InputChannelCount, MasteringVoice.VoiceDetails.InputChannelCount);
            X3DAudio.Calculate(Listener, emitter, CalculateFlags.Matrix | CalculateFlags.Doppler | CalculateFlags.LpfDirect | CalculateFlags.Reverb, settings);
            voice.SetOutputMatrix(MasteringVoice, voice.VoiceDetails.InputChannelCount, MasteringVoice.VoiceDetails.InputChannelCount, settings.MatrixCoefficients);
            voice.SetFrequencyRatio(settings.DopplerFactor, IXAudio2.CommitNow);
            FilterParameters parameters = new() { Type = FilterType.LowPassFilter, Frequency = 2.0f * MathF.Sin(MathF.PI / 6.0f * settings.LpfDirectCoefficient), OneOverQ = 1.0f };
            voice.SetFilterParameters(parameters, IXAudio2.CommitNow);
        }

        protected override void Dispose(bool disposing)
        {
            X3DAudio = null;
            SubmixVoice.Dispose();
            SubmixVoice = null;
            MasteringVoice.Dispose();
            MasteringVoice = null;
            IXAudio2.StopEngine();
            IXAudio2.Dispose();
            IXAudio2 = null;
            base.Dispose(disposing);
        }
    }
}