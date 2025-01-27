namespace VoxelEngine.Core
{
    using Newtonsoft.Json;

    public class Settings
    {
        [JsonProperty]
        public bool VSync = false;

        [JsonProperty]
        public bool LimitFPS = true;

        [JsonProperty]
        public int TargetFPS = 120;

        [JsonProperty]
        public int ShadowMapSize = 1024 * 2;

        [JsonProperty]
        public bool ShaderCache = false;

        [JsonProperty]
        public int ChunkRenderDistance { get; set; } = 32;

        [JsonProperty]
        public int ChunkSimulationDistance { get; set; } = 8;

        [JsonIgnore]
        public int BufferCount = 2;

        [JsonIgnore]
        public bool MSAA = false;

        [JsonIgnore]
        public int MSAASampleCount = 1;

        [JsonIgnore]
        public int MSAASampleQuality;

        [JsonIgnore]
        public float MaxDepth = 1000f;

        [JsonIgnore]
        public float MinDepth = .001f;

        internal void Save()
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public static class Nucleus
    {
        static Nucleus()
        {
            if (File.Exists("config.json"))
            {
                Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("config.json"));
            }
            else
            {
                Settings = new();
                Settings.Save();
            }
        }

        public static Settings Settings { get; }
    }
}