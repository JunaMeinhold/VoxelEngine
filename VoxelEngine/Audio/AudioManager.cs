namespace VoxelEngine.Audio
{
    using System.Collections.Generic;
    using Vortice.Multimedia;
    using Vortice.XAudio2;

    public static class AudioManager
    {
        public static X3DAudio X3DAudio { get; private set; }

        public static IXAudio2 IXAudio2 { get; private set; }

        public static MasteringVoice MasteringVoice { get; set; }

        public static SubmixVoice SubmixVoice { get; set; }

        public static List<VoiceGroup> VoiceGroups { get; } = new();

        static AudioManager()
        {
            IXAudio2 = XAudio2.XAudio2Create(ProcessorSpecifier.UseDefaultProcessor);
            IXAudio2.StartEngine();
            MasteringVoice = new();
            SubmixVoice = new();
            VoiceGroups.Add(MasteringVoice);
            X3DAudio = new((Speakers)MasteringVoice.Audio2MasteringVoice.ChannelMask);
        }

        public static VoiceGroup GetVoiceGroup(string name)
        {
            return VoiceGroups.FirstOrDefault(x => x.Name == name);
        }

        public static IEnumerable<VoiceSendDescriptor> GetVoiceSendDescriptors(IEnumerable<string> names)
        {
            foreach (string name in names)
            {
                yield return GetVoiceGroupDescriptor(name);
            }
        }

        public static VoiceSendDescriptor GetVoiceGroupDescriptor(string name)
        {
            var group = VoiceGroups.FirstOrDefault(x => x.Name == name);
            if (group == null)
            {
                return default;
            }
            else
            {
                var desc = new VoiceSendDescriptor()
                {
                    Flags = 0,
                    OutputVoice = group.Voice,
                };
                return desc;
            }
        }

        public static void Dispose()
        {
            X3DAudio = null;
            MasteringVoice.Dispose();
            IXAudio2.StopEngine();
            IXAudio2.Dispose();
            IXAudio2 = null;
        }
    }
}