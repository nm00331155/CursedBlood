using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace CursedBlood.Core
{
    public static class JsonStorage
    {
        public static T Load<T>(string path, Func<T> fallbackFactory) where T : class
        {
            try
            {
                if (!Godot.FileAccess.FileExists(path))
                {
                    return fallbackFactory();
                }

                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    return fallbackFactory();
                }

                var json = file.GetAsText();
                return JsonSerializer.Deserialize<T>(json) ?? fallbackFactory();
            }
            catch (Exception exception)
            {
                GD.PrintErr($"Failed to load json from {path}: {exception.Message}");
                return fallbackFactory();
            }
        }

        public static void Save<T>(string path, T data)
        {
            try
            {
                var absolutePath = ProjectSettings.GlobalizePath(path);
                var directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    GD.PrintErr($"Failed to open json file for writing: {path}");
                    return;
                }

                file.StoreString(json);
            }
            catch (Exception exception)
            {
                GD.PrintErr($"Failed to save json to {path}: {exception.Message}");
            }
        }
    }
}