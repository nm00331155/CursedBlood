using System;
using System.Text.Json;
using Godot;

namespace CursedBlood.Core
{
    public enum ThemeMode
    {
        Dark,
        Light
    }

    public readonly struct GameTheme
    {
        public GameTheme(
            ThemeMode mode,
            Color backgroundColor,
            Color panelColor,
            Color textColor,
            Color accentColor)
        {
            Mode = mode;
            BackgroundColor = backgroundColor;
            PanelColor = panelColor;
            TextColor = textColor;
            AccentColor = accentColor;
            EmptyCellColor = backgroundColor.Lerp(textColor, mode == ThemeMode.Dark ? 0.08f : 0.03f);
            NormalCellColor = new Color(0.58f, 0.38f, 0.22f).Lerp(accentColor, 0.18f);
            HardCellColor = backgroundColor.Lerp(textColor, mode == ThemeMode.Dark ? 0.26f : 0.18f);
            IndestructibleColor = backgroundColor.Lerp(textColor, mode == ThemeMode.Dark ? 0.15f : 0.10f);
            GridLineColor = new Color(textColor.R, textColor.G, textColor.B, mode == ThemeMode.Dark ? 0.22f : 0.18f);
            BorderColor = accentColor.Lerp(textColor, 0.40f);
            WarningColor = accentColor.Lerp(new Color(1f, 0.18f, 0.18f), 0.65f);
            OverlayColor = mode == ThemeMode.Dark
                ? new Color(0f, 0f, 0f, 0.86f)
                : new Color(0.06f, 0.07f, 0.09f, 0.74f);
            PlayerYouthColor = accentColor.Lerp(new Color(0.32f, 0.86f, 0.40f), 0.55f);
            PlayerPrimeColor = accentColor;
            PlayerTwilightColor = accentColor.Lerp(new Color(0.90f, 0.45f, 0.20f), 0.68f);
        }

        public ThemeMode Mode { get; }

        public Color BackgroundColor { get; }

        public Color PanelColor { get; }

        public Color TextColor { get; }

        public Color AccentColor { get; }

        public Color EmptyCellColor { get; }

        public Color NormalCellColor { get; }

        public Color HardCellColor { get; }

        public Color IndestructibleColor { get; }

        public Color GridLineColor { get; }

        public Color BorderColor { get; }

        public Color WarningColor { get; }

        public Color OverlayColor { get; }

        public Color PlayerYouthColor { get; }

        public Color PlayerPrimeColor { get; }

        public Color PlayerTwilightColor { get; }
    }

    public sealed class ThemeSettings
    {
        private static readonly string[] DarkBackgrounds =
        {
            "#14161D",
            "#18201B",
            "#201813"
        };

        private static readonly string[] LightBackgrounds =
        {
            "#F1E8D8",
            "#E3EEE8",
            "#EEE5F3"
        };

        private static readonly string[] DarkTexts =
        {
            "#F6F3EA",
            "#E8F0F4",
            "#F5EAD8"
        };

        private static readonly string[] LightTexts =
        {
            "#1C1B1A",
            "#182226",
            "#2A211A"
        };

        private static readonly string[] Accents =
        {
            "#D4643A",
            "#2AA39A",
            "#C9A227",
            "#B24D6A"
        };

        public ThemeMode Mode { get; private set; } = ThemeMode.Dark;

        public int BackgroundIndex { get; private set; }

        public int TextIndex { get; private set; }

        public int AccentIndex { get; private set; }

        public int BackgroundOptionsCount => GetBackgroundPalette().Length;

        public int TextOptionsCount => GetTextPalette().Length;

        public int AccentOptionsCount => Accents.Length;

        public static ThemeSettings CreateDefault()
        {
            return new ThemeSettings();
        }

        public GameTheme BuildTheme()
        {
            ClampIndices();

            var backgroundColor = Color.FromString(GetBackgroundPalette()[BackgroundIndex], new Color(0.08f, 0.09f, 0.11f));
            var textColor = Color.FromString(GetTextPalette()[TextIndex], Colors.White);
            var accentColor = Color.FromString(Accents[AccentIndex], new Color(0.83f, 0.40f, 0.22f));
            var panelColor = Mode == ThemeMode.Dark
                ? backgroundColor.Lerp(Colors.Black, 0.24f)
                : backgroundColor.Lerp(Colors.White, 0.10f);

            return new GameTheme(Mode, backgroundColor, panelColor, textColor, accentColor);
        }

        public void ToggleMode()
        {
            Mode = Mode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
            ClampIndices();
        }

        public void CycleBackground()
        {
            BackgroundIndex = WrapIndex(BackgroundIndex + 1, GetBackgroundPalette().Length);
        }

        public void CycleText()
        {
            TextIndex = WrapIndex(TextIndex + 1, GetTextPalette().Length);
        }

        public void CycleAccent()
        {
            AccentIndex = WrapIndex(AccentIndex + 1, Accents.Length);
        }

        internal ThemeSettingsData ToData()
        {
            return new ThemeSettingsData
            {
                Mode = Mode.ToString(),
                BackgroundIndex = BackgroundIndex,
                TextIndex = TextIndex,
                AccentIndex = AccentIndex
            };
        }

        internal static ThemeSettings FromData(ThemeSettingsData data)
        {
            var settings = new ThemeSettings();

            if (data == null)
            {
                return settings;
            }

            if (Enum.TryParse(data.Mode, true, out ThemeMode parsedMode))
            {
                settings.Mode = parsedMode;
            }

            settings.BackgroundIndex = data.BackgroundIndex;
            settings.TextIndex = data.TextIndex;
            settings.AccentIndex = data.AccentIndex;
            settings.ClampIndices();
            return settings;
        }

        private string[] GetBackgroundPalette()
        {
            return Mode == ThemeMode.Dark ? DarkBackgrounds : LightBackgrounds;
        }

        private string[] GetTextPalette()
        {
            return Mode == ThemeMode.Dark ? DarkTexts : LightTexts;
        }

        private void ClampIndices()
        {
            BackgroundIndex = WrapIndex(BackgroundIndex, GetBackgroundPalette().Length);
            TextIndex = WrapIndex(TextIndex, GetTextPalette().Length);
            AccentIndex = WrapIndex(AccentIndex, Accents.Length);
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return ((value % count) + count) % count;
        }
    }

    internal sealed class ThemeSettingsData
    {
        public string Mode { get; set; } = ThemeMode.Dark.ToString();

        public int BackgroundIndex { get; set; }

        public int TextIndex { get; set; }

        public int AccentIndex { get; set; }
    }

    public static class ThemeSettingsStore
    {
        private const string SettingsDirectory = "user://settings";
        private const string SettingsPath = "user://settings/theme_settings.json";

        public static ThemeSettings Load()
        {
            try
            {
                using var file = Godot.FileAccess.Open(SettingsPath, Godot.FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    return ThemeSettings.CreateDefault();
                }

                var json = file.GetAsText();
                var data = JsonSerializer.Deserialize<ThemeSettingsData>(json);
                return ThemeSettings.FromData(data);
            }
            catch (Exception exception)
            {
                GD.PrintErr($"Failed to load theme settings: {exception.Message}");
                return ThemeSettings.CreateDefault();
            }
        }

        public static void Save(ThemeSettings settings)
        {
            try
            {
                DirAccess.MakeDirAbsolute(ProjectSettings.GlobalizePath(SettingsDirectory));
                var json = JsonSerializer.Serialize(settings.ToData(), new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                using var file = Godot.FileAccess.Open(SettingsPath, Godot.FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    GD.PrintErr("Failed to open theme settings for writing.");
                    return;
                }

                file.StoreString(json);
            }
            catch (Exception exception)
            {
                GD.PrintErr($"Failed to save theme settings: {exception.Message}");
            }
        }
    }
}