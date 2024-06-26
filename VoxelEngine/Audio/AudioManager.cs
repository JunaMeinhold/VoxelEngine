namespace VoxelEngine.Audio
{
    using System.Collections.Generic;
    using Hexa.NET.X3DAudio;
    using Hexa.NET.XAudio2;
    using HexaGen.Runtime.COM;

    public static unsafe class AudioManager
    {
        public static X3DAudioHandle X3DAudioHandle { get; private set; }

        public static ComPtr<IXAudio2> IXAudio2 { get; private set; }

        public static MasteringVoice MasteringVoice { get; set; }

        public static SubmixVoice SubmixVoice { get; set; }

        public static List<VoiceGroup> VoiceGroups { get; } = new();

        static AudioManager()
        {
            IXAudio2* comPtr = default;
            XAudio2.XAudio2CreateWithVersionInfo(&comPtr, 0, XAudio2.XAudio2_USE_DEFAULT_PROCESSOR, 0);
            IXAudio2 = comPtr;
            IXAudio2.StartEngine();
            MasteringVoice = new();
            SubmixVoice = new();
            VoiceGroups.Add(MasteringVoice);

            uint channelMask = 0;
            MasteringVoice.Audio2MasteringVoice.GetChannelMask(&channelMask);

            X3DAudioHandle handle = new();
            X3DAudio.X3DAudioInitialize(channelMask, X3DAudio.X3DAudio_SPEED_OF_SOUND, &handle);
        }

        public static VoiceGroup GetVoiceGroup(string name)
        {
            return VoiceGroups.FirstOrDefault(x => x.Name == name);
        }

        /*
        public static IEnumerable<XAudio2VoiceSends> GetVoiceSendDescriptors(IEnumerable<string> names)
        {
            foreach (string name in names)
            {
                yield return GetVoiceGroupDescriptor(name);
            }
        }

        public static XAudio2VoiceSends GetVoiceGroupDescriptor(string name)
        {
            var group = VoiceGroups.FirstOrDefault(x => x.Name == name);
            if (group == null)
            {
                return default;
            }
            else
            {
                var desc = new XAudio2VoiceSends()
                {
                    Flags = 0,
                    OutputVoice = group.Voice,
                };
                return desc;
            }
        }
        */

        public static void Dispose()
        {
            MasteringVoice.Dispose();
            IXAudio2.StopEngine();
            IXAudio2.Dispose();
            IXAudio2 = null;
        }
    }
}