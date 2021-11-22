using HexaEngine.Fonts;
using HexaEngine.Shaders;
using HexaEngine.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexaEngine.Resources
{
    public static class ResourceManager
    {
        private static readonly Dictionary<ResourceState, Texture> textures = new();
        private static readonly Dictionary<ResourceState, Model> models = new();
        private static readonly Dictionary<ResourceState, Sound> sounds = new();
        private static readonly Dictionary<ResourceState, AtlasFont> fonts = new();
        private static readonly Dictionary<ResourceState, Shader> shaders = new();

        public static string CurrentTexturePath { get; set; } = "assets/textures/";

        public static string CurrentModelPath { get; set; } = "assets/models/";

        public static string CurrentShaderPath { get; set; } = "assets/shaders/";

        public static string CurrentFontPath { get; set; } = "assets/fonts/";

        public static string CurrentSoundPath { get; set; } = "assets/sounds/";

        public static void ReleaseAll()
        {
            foreach (var pair in textures)
            {
                pair.Value.Dispose();
            }
            foreach (var pair in models)
            {
                pair.Value.Dispose();
            }
            foreach (var pair in sounds)
            {
                pair.Value.Dispose();
            }
        }

        public static Texture LoadTexture(string path, bool isFont = false)
        {
            var path1 = (isFont ? CurrentFontPath : CurrentTexturePath) + path;
            var resource = textures.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return resource.Value;
            }
            else
            {
                Texture texture = new();
                texture.Load(DeviceManager.Current.ID3D11Device, path1);
                textures.Add(new ResourceState() { Instances = 1, Path = path1 }, texture);
                return texture;
            }
        }

        public static Texture LoadTexture(string[] paths)
        {
            var path1 = string.Join(' ', paths);
            var resource = textures.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return resource.Value;
            }
            else
            {
                Texture texture = new();
                texture.Load(DeviceManager.Current.ID3D11Device, paths);
                textures.Add(new ResourceState() { Instances = 1, Path = path1 }, texture);
                return texture;
            }
        }

        public static Texture LoadCubeMapTexture(string path)
        {
            var path1 = CurrentTexturePath + path;
            var resource = textures.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return resource.Value;
            }
            else
            {
                Texture texture = new();
                texture.LoadCubeMap(DeviceManager.Current.ID3D11Device, path1);
                textures.Add(new ResourceState() { Instances = 1, Path = path1 }, texture);
                return texture;
            }
        }

        public static Model LoadModelObj(string path)
        {
            var path1 = CurrentModelPath + path;
            var resource = models.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return resource.Value;
            }
            else
            {
                Model model = new();
                model.LoadObj(DeviceManager.Current, path1);
                models.Add(new ResourceState() { Instances = 1, Path = path1 }, model);
                return model;
            }
        }

        public static Sound LoadSound(string path)
        {
            var path1 = new FileInfo(CurrentSoundPath + path).FullName;
            var resource = sounds.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return resource.Value;
            }
            else
            {
                Sound sound = new();
                sound.LoadAudioFile(DeviceManager.Current.AudioManager, path1);
                sounds.Add(new ResourceState() { Instances = 1, Path = path1 }, sound);
                return sound;
            }
        }

        public static AtlasFont LoadFont(string path)
        {
            var path1 = new FileInfo(CurrentSoundPath + path).FullName + new FileInfo(CurrentFontPath).FullName;
            var resource = fonts.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return resource.Value;
            }
            else
            {
                AtlasFont font = new(CurrentFontPath + path);
                fonts.Add(new ResourceState() { Instances = 1, Path = path1 }, font);
                return font;
            }
        }

        public static T LoadShader<T>() where T : Shader, new()
        {
            var path1 = typeof(T).FullName;
            var resource = shaders.FirstOrDefault(x => x.Key.Path == path1);
            if (resource.Value is not null)
            {
                resource.Key.Instances++;
                return (T)resource.Value;
            }
            else
            {
                T shader = new T();
                shaders.Add(new ResourceState() { Instances = 1, Path = path1 }, shader);
                return shader;
            }
        }
    }
}