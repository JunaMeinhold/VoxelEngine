namespace VoxelEngine.Audio
{
    using Vortice.XAudio2;

    public class VoiceGroup
    {
        public string Name { get; set; }

        public IXAudio2Voice Voice { get; set; }
    }
}