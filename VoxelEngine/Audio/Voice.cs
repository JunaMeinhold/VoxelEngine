namespace VoxelEngine.Audio
{
    using Vortice.XAudio2;

    public class Voice
    {
        public string Name { get; set; }

        public IXAudio2Voice Audio2Voice { get; set; }
    }
}