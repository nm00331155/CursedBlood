using System.IO;
using System.Text.Json;
using Godot;

namespace CursedBlood.Save
{
    public sealed class SaveManager
    {
        private const string SaveFilePath = "user://cursedblood_save.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public SaveData Load()
        {
            if (!Exists())
            {
                var created = CreateNew();
                Save(created);
                return created;
            }

            try
            {
                var fullPath = ProjectSettings.GlobalizePath(SaveFilePath);
                var json = File.ReadAllText(fullPath);
                var loaded = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
                return Migrate(loaded);
            }
            catch
            {
                var fallback = CreateNew();
                Save(fallback);
                return fallback;
            }
        }

        public void Save(SaveData data)
        {
            var safeData = Migrate(data);
            safeData.Meta.UpdatedAt = DateTimeOffset.UtcNow;

            var fullPath = ProjectSettings.GlobalizePath(SaveFilePath);
            var directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var json = JsonSerializer.Serialize(safeData, JsonOptions);
            File.WriteAllText(fullPath, json);
        }

        public SaveData CreateNew()
        {
            return Migrate(new SaveData());
        }

        public SaveData Migrate(SaveData oldData)
        {
            var data = oldData ?? new SaveData();
            data.Meta ??= new SaveMeta();
            data.PlayerProfile ??= new PlayerProfileData();
            data.Debt ??= new DebtData();
            data.Research ??= new ResearchData();
            data.Equipment ??= new EquipmentData();
            data.Records ??= new RecordsData();
            data.Achievement ??= new AchievementData();
            data.Ranking ??= new RankingData();
            data.Settings ??= new SettingsData();

            data.Settings.VirtualPadOpacity = Mathf.Clamp(data.Settings.VirtualPadOpacity, 0.10f, 0.85f);
            data.Debt.CurrentDebt = Math.Max(0L, data.Debt.CurrentDebt);
            data.PlayerProfile.CurrentMoney = Math.Max(0L, data.PlayerProfile.CurrentMoney);
            data.PlayerProfile.TotalDiveCount = Mathf.Max(0, data.PlayerProfile.TotalDiveCount);
            data.PlayerProfile.IsProfileConfigured |= data.PlayerProfile.Gender != "Unknown" || data.PlayerProfile.Name != "Diver";
            data.Meta.Version = 1;

            return data;
        }

        public bool Exists()
        {
            var fullPath = ProjectSettings.GlobalizePath(SaveFilePath);
            return File.Exists(fullPath);
        }

        public void DeleteAll()
        {
            var fullPath = ProjectSettings.GlobalizePath(SaveFilePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}