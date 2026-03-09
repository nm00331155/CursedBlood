using System.Collections.Generic;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Audio
{
    public sealed class AudioSettingsData
    {
        public float BgmVolume { get; set; } = 0.8f;

        public float SeVolume { get; set; } = 0.8f;

        public bool VibrationEnabled { get; set; } = true;
    }

    public partial class AudioManager : Node
    {
        private const string SavePath = "user://settings/audio_settings.json";

        private readonly Dictionary<string, string> _bgmPaths = new()
        {
            ["title"] = "res://Audio/BGM/title.ogg",
            ["play"] = "res://Audio/BGM/play.ogg",
            ["boss"] = "res://Audio/BGM/boss.ogg",
            ["ending"] = "res://Audio/BGM/ending.ogg"
        };

        private readonly Dictionary<string, string> _sePaths = new()
        {
            ["dig"] = "res://Audio/SE/dig.wav",
            ["kill"] = "res://Audio/SE/kill.wav",
            ["drop"] = "res://Audio/SE/drop.wav",
            ["skill"] = "res://Audio/SE/skill.wav"
        };

        public AudioSettingsData Settings { get; private set; } = new();

        public override void _Ready()
        {
            Settings = JsonStorage.Load(SavePath, () => new AudioSettingsData());
        }

        public void SaveSettings()
        {
            JsonStorage.Save(SavePath, Settings);
        }

        public void AdjustBgm(float delta)
        {
            Settings.BgmVolume = Mathf.Clamp(Settings.BgmVolume + delta, 0f, 1f);
            SaveSettings();
        }

        public void AdjustSe(float delta)
        {
            Settings.SeVolume = Mathf.Clamp(Settings.SeVolume + delta, 0f, 1f);
            SaveSettings();
        }

        public void ToggleVibration()
        {
            Settings.VibrationEnabled = !Settings.VibrationEnabled;
            SaveSettings();
        }

        public string GetBgmPath(string key)
        {
            return _bgmPaths.TryGetValue(key, out var path) ? path : string.Empty;
        }

        public string GetSePath(string key)
        {
            return _sePaths.TryGetValue(key, out var path) ? path : string.Empty;
        }

        public void PlayBgm(string key)
        {
            GD.Print($"PlayBgm: {GetBgmPath(key)} @ {Settings.BgmVolume:F2}");
        }

        public void PlaySe(string key)
        {
            GD.Print($"PlaySe: {GetSePath(key)} @ {Settings.SeVolume:F2}");
        }
    }
}