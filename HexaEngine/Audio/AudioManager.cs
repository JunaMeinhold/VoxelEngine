using Vortice.Multimedia;
using Vortice.XAudio2;

namespace HexaEngine.Audio
{
    /// <summary>
    /// Basics sind implementiert aber mehr auch nicht.
    /// </summary>
    public class AudioManager
    {
        public X3DAudio X3DAudio { get; } = new(Speakers.All);

        public IXAudio2 IXAudio2 { get; }

        public Listener Listener { get; } = new Listener();

        public IXAudio2MasteringVoice MasteringVoice { get; }

        public int Channels { get; set; }

        public AudioManager()
        {
            IXAudio2 = XAudio2.XAudio2Create(ProcessorSpecifier.UseDefaultProcessor);
            MasteringVoice = IXAudio2.CreateMasteringVoice(0, 0, AudioStreamCategory.GameMedia);
            /*
            var listener = new Listener();
            listener.OrientFront = System.Numerics.Vector3.UnitZ;
            listener.OrientTop = System.Numerics.Vector3.UnitY;
            listener.Position = System.Numerics.Vector3.Zero;
            listener.Velocity = System.Numerics.Vector3.Zero;
            Emitter emitter = new();
            emitter.Position = new System.Numerics.Vector3(0, 0, 10);
            var settings = X3DAudio.Calculate(listener, emitter, CalculateFlags.None, 0, 0);*/
        }

        public void Play(IXAudio2SourceVoice voice, Emitter emitter, CalculateFlags flags)
        {
        }
    }
}