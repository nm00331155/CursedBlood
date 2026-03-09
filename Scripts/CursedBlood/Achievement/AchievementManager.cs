using System;
using System.Collections.Generic;
using System.Linq;
using CursedBlood.Core;
using CursedBlood.Curse;
using CursedBlood.Debt;
using CursedBlood.Generation;
using CursedBlood.Player;

namespace CursedBlood.Achievement
{
    public sealed class AchievementManager
    {
        private const string SavePath = "user://achievements.json";
        private const float CurseOvercomerSecondsThreshold = 60f * 40f / 35f;

        private static readonly IReadOnlyDictionary<string, string> LegacyIdMap = new Dictionary<string, string>
        {
            ["kill_10"] = "first_battle",
            ["boss_1"] = "boss_hunter",
            ["legendary_1"] = "treasure_hunter",
            ["family_1"] = "bloodline_start",
            ["family_10"] = "family_pride",
            ["debt_clear"] = "debt_free"
        };

        public AchievementCounters Counters { get; set; } = new();

        public List<AchievementEntry> Entries { get; set; } = CreateDefaultEntries();

        public static AchievementManager Load()
        {
            var manager = JsonStorage.Load(SavePath, () => new AchievementManager());
            manager.Counters ??= new AchievementCounters();
            manager.Entries = MergeWithDefaults(manager.Entries);
            return manager;
        }

        public void Save()
        {
            JsonStorage.Save(SavePath, this);
        }

        public IReadOnlyList<AchievementEntry> GetEntries(AchievementCategory? category = null)
        {
            if (category == null)
            {
                return Entries;
            }

            return Entries.Where(entry => entry.Category == category.Value).ToList();
        }

        public List<AchievementEntry> CheckAndUnlock(PlayerStats stats, FamilyTree familyTree, DebtManager debtManager, CurseResearchManager curseResearch)
        {
            var newlyUnlocked = new List<AchievementEntry>();
            var currentGeneration = Math.Max(stats.Generation, familyTree.Records.Count + 1);

            EvaluateEntry("dig_talent", stats.MaxDepth / 500f, stats.MaxDepth >= 500, newlyUnlocked);
            EvaluateEntry("ore_nose", Counters.TotalOresBroken / 100f, Counters.TotalOresBroken >= 100, newlyUnlocked);
            EvaluateEntry("rock_breaker", Counters.TotalHardBlocksBroken / 5000f, Counters.TotalHardBlocksBroken >= 5000, newlyUnlocked);
            EvaluateEntry("deep_dweller", stats.MaxDepth / 5000f, stats.MaxDepth >= 5000, newlyUnlocked);
            EvaluateEntry("earth_king", stats.MaxDepth / 10000f, stats.MaxDepth >= 10000, newlyUnlocked);

            EvaluateEntry("first_battle", Counters.TotalEnemiesKilled / 10f, Counters.TotalEnemiesKilled >= 10, newlyUnlocked);
            EvaluateEntry("combo_master", stats.MaxCombo / 50f, stats.MaxCombo >= 50, newlyUnlocked);
            EvaluateEntry("boss_hunter", stats.BossesKilled, stats.BossesKilled >= 1, newlyUnlocked);
            EvaluateEntry("iron_wall", Counters.TotalGuardBlocks / 100f, Counters.TotalGuardBlocks >= 100, newlyUnlocked);
            EvaluateEntry("hundred_slayer", stats.EnemiesKilled / 100f, stats.EnemiesKilled >= 100, newlyUnlocked);

            EvaluateEntry("collector", Counters.TotalEquipmentFound / 50f, Counters.TotalEquipmentFound >= 50, newlyUnlocked);
            EvaluateEntry("appraiser", Counters.TotalRarePlusFound / 10f, Counters.TotalRarePlusFound >= 10, newlyUnlocked);
            EvaluateEntry("rich", stats.LifetimeGold / 10000f, stats.LifetimeGold >= 10000, newlyUnlocked);
            EvaluateEntry("treasure_hunter", Counters.TotalLegendaryFound, Counters.TotalLegendaryFound >= 1, newlyUnlocked);
            EvaluateEntry("curse_collector", Counters.TotalCursedFound / 3f, Counters.TotalCursedFound >= 3, newlyUnlocked);

            EvaluateEntry("bloodline_start", currentGeneration / 2f, currentGeneration >= 2, newlyUnlocked);
            EvaluateEntry("family_pride", currentGeneration / 10f, currentGeneration >= 10, newlyUnlocked);
            EvaluateEntry("debt_free", debtManager.IsCleared ? 1f : 0f, debtManager.IsCleared, newlyUnlocked);
            EvaluateEntry("inheritor", Counters.TotalGoldInherited / 100000f, Counters.TotalGoldInherited >= 100000, newlyUnlocked);
            EvaluateEntry("curse_overcomer", stats.MaxLifespan / CurseOvercomerSecondsThreshold, stats.MaxLifespan >= CurseOvercomerSecondsThreshold, newlyUnlocked);

            return newlyUnlocked;
        }

        public void IncrementCounter(CounterType counterType, long amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            switch (counterType)
            {
                case CounterType.OresBroken:
                    Counters.TotalOresBroken += amount;
                    break;
                case CounterType.HardBlocksBroken:
                    Counters.TotalHardBlocksBroken += amount;
                    break;
                case CounterType.EnemiesKilled:
                    Counters.TotalEnemiesKilled += amount;
                    break;
                case CounterType.GuardBlocks:
                    Counters.TotalGuardBlocks += amount;
                    break;
                case CounterType.EquipmentFound:
                    Counters.TotalEquipmentFound += amount;
                    break;
                case CounterType.RarePlusFound:
                    Counters.TotalRarePlusFound += amount;
                    break;
                case CounterType.LegendaryFound:
                    Counters.TotalLegendaryFound += amount;
                    break;
                case CounterType.CursedFound:
                    Counters.TotalCursedFound += amount;
                    break;
                case CounterType.GoldInherited:
                    Counters.TotalGoldInherited += amount;
                    break;
            }
        }

        public AchievementBonuses GetBonuses()
        {
            var bonuses = new AchievementBonuses();
            foreach (var entry in Entries.Where(entry => entry.Unlocked))
            {
                bonuses.Merge(entry.Bonus);
            }

            return bonuses;
        }

        public int GetUnlockedCount()
        {
            return Entries.Count(entry => entry.Unlocked);
        }

        private void EvaluateEntry(string id, float progress, bool shouldUnlock, List<AchievementEntry> newlyUnlocked)
        {
            var entry = Entries.First(candidate => candidate.Id == id);
            entry.Progress = (float)Math.Clamp(progress, 0d, 1d);
            if (entry.Unlocked || !shouldUnlock)
            {
                return;
            }

            entry.Unlocked = true;
            entry.Progress = 1f;
            newlyUnlocked.Add(entry);
        }

        private static List<AchievementEntry> MergeWithDefaults(List<AchievementEntry> persistedEntries)
        {
            var mergedEntries = CreateDefaultEntries();
            if (persistedEntries == null || persistedEntries.Count == 0)
            {
                return mergedEntries;
            }

            var persistedLookup = new Dictionary<string, AchievementEntry>();
            foreach (var entry in persistedEntries)
            {
                var resolvedId = ResolveLegacyId(entry.Id);
                if (!persistedLookup.ContainsKey(resolvedId))
                {
                    persistedLookup[resolvedId] = entry;
                }
            }

            foreach (var entry in mergedEntries)
            {
                if (!persistedLookup.TryGetValue(entry.Id, out var persistedEntry))
                {
                    continue;
                }

                entry.Unlocked = persistedEntry.Unlocked;
                entry.Progress = persistedEntry.Progress;
            }

            return mergedEntries;
        }

        private static string ResolveLegacyId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            return LegacyIdMap.TryGetValue(id, out var mappedId) ? mappedId : id;
        }

        private static List<AchievementEntry> CreateDefaultEntries()
        {
            return new List<AchievementEntry>
            {
                Create("dig_talent", "掘りの才能", "1プレイで深度500m到達", "掘削速度+5%", AchievementCategory.Digging, new AchievementBonuses { DigSpeedBonus = 0.05f }),
                Create("ore_nose", "鉱脈の嗅覚", "鉱石ブロック累計100個破壊", "鉱石可視範囲+5セル", AchievementCategory.Digging, new AchievementBonuses { OreVisionBonus = 5 }),
                Create("rock_breaker", "岩砕き", "石/硬岩ブロック累計5000個破壊", "硬ブロック追加ダメージ+10%", AchievementCategory.Digging, new AchievementBonuses { HardBlockBonus = 0.10f }),
                Create("deep_dweller", "深淵の住人", "1プレイで深度5000m到達", "移動速度+5%", AchievementCategory.Digging, new AchievementBonuses { MoveSpeedBonus = 0.05f }),
                Create("earth_king", "地底王", "1プレイで深度10000m到達", "掘削速度+15%", AchievementCategory.Digging, new AchievementBonuses { DigSpeedBonus = 0.15f }),

                Create("first_battle", "初陣", "敵を累計10体撃破", "攻撃力+5%", AchievementCategory.Combat, new AchievementBonuses { DigPowerMultiplier = 1.05f }),
                Create("combo_master", "コンボマスター", "1プレイで50コンボ達成", "コンボ維持時間+1秒", AchievementCategory.Combat, new AchievementBonuses { ComboTimerBonus = 1f }),
                Create("boss_hunter", "ボスハンター", "ボスを1体撃破", "ボスダメージ+10%", AchievementCategory.Combat, new AchievementBonuses { BossDamageBonus = 0.10f }),
                Create("iron_wall", "鉄壁", "ガードで弾を累計100回防ぐ", "被ダメ-5%", AchievementCategory.Combat, new AchievementBonuses { DamageReductionBonus = 0.05f }),
                Create("hundred_slayer", "百人斬り", "1プレイで敵100体撃破", "クリティカル率+3%", AchievementCategory.Combat, new AchievementBonuses { CritRateBonus = 0.03f }),

                Create("collector", "拾い屋", "装備を累計50個拾う", "ドロップ率+5%", AchievementCategory.Collection, new AchievementBonuses { DropRateBonus = 0.05f }),
                Create("appraiser", "目利き", "Rare以上の装備を累計10個入手", "Rare以上ドロップ率+3%", AchievementCategory.Collection, new AchievementBonuses { DropRateBonus = 0.03f }),
                Create("rich", "金持ち", "1プレイでゴールド10,000G獲得", "ゴールド取得+10%", AchievementCategory.Collection, new AchievementBonuses { GoldBonus = 0.10f }),
                Create("treasure_hunter", "お宝ハンター", "Legendary装備を1個入手", "ドロップ率+10%", AchievementCategory.Collection, new AchievementBonuses { DropRateBonus = 0.10f }),
                Create("curse_collector", "呪いコレクター", "Cursed装備を累計3個入手", "呪い研究度獲得量+20%", AchievementCategory.Collection, new AchievementBonuses { CurseResearchBonus = 0.20f }),

                Create("bloodline_start", "血脈の始まり", "第2世代に到達", "初期HP+10", AchievementCategory.Generation, new AchievementBonuses { MaxHpBonus = 10 }),
                Create("family_pride", "一族の意地", "第10世代に到達", "全ステ+3%", AchievementCategory.Generation, new AchievementBonuses { AllStatsMultiplier = 1.03f }),
                Create("debt_free", "借金完済", "借金を完済する", "全ステ+5%", AchievementCategory.Generation, new AchievementBonuses { AllStatsMultiplier = 1.05f }),
                Create("inheritor", "遺産家", "遺産で累計100,000G引き継ぐ", "遺産率35%に上昇", AchievementCategory.Generation, new AchievementBonuses { InheritanceRateOverride = 0.35f }),
                Create("curse_overcomer", "呪いの克服者", "寿命を40歳以上にする", "晩年/少年期の能力低下を軽減", AchievementCategory.Generation, new AchievementBonuses { YouthMultiplierOverride = 0.8f, TwilightMultiplierOverride = 0.85f })
            };
        }

        private static AchievementEntry Create(string id, string title, string description, string passiveDescription, AchievementCategory category, AchievementBonuses bonus)
        {
            return new AchievementEntry
            {
                Id = id,
                Title = title,
                Description = description,
                PassiveDescription = passiveDescription,
                Category = category,
                Bonus = bonus
            };
        }
    }
}