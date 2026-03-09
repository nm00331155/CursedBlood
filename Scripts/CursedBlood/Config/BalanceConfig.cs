using System;
using CursedBlood.Core;
using CursedBlood.Equipment;
using Godot;

namespace CursedBlood.Config
{
    public sealed class BalanceConfig
    {
        private const string SavePath = "user://balance.json";

        public float LifespanSeconds { get; set; } = 60f;

        public float BaseMoveSpeed { get; set; } = 0.3f;

        public float MinimumMoveSpeed { get; set; } = 0.08f;

        public float YouthPhaseMultiplier { get; set; } = 0.6f;

        public float PrimePhaseMultiplier { get; set; } = 1f;

        public float TwilightPhaseMultiplier { get; set; } = 0.7f;

        public float BulletCellDuration { get; set; } = 0.5f;

        public float ComboResetSeconds { get; set; } = 3f;

        public float EquipmentDropChance { get; set; } = 0.25f;

        public float BossBaseHp { get; set; } = 500f;

        public float BossHpGrowth { get; set; } = 1.5f;

        public float DemonLordHp { get; set; } = 999999f;

        public float CollectorGoldStealRatio { get; set; } = 0.2f;

        public float SkillChargePerDig { get; set; } = 1f;

        public float SkillChargePerKill { get; set; } = 20f;

        public float SkillChargePerBossHit { get; set; } = 5f;

        public static BalanceConfig Load()
        {
            var config = JsonStorage.Load(SavePath, () => new BalanceConfig());
            config.Save();
            return config;
        }

        public void Save()
        {
            JsonStorage.Save(SavePath, this);
        }

        public float GetEnemySpawnChance(int depth)
        {
            return Mathf.Clamp(0.05f + depth / 1000f * 0.15f, 0.05f, 0.20f);
        }

        public float GetOreSpawnChance(int depth)
        {
            return Mathf.Clamp(0.08f + depth / 1000f * 0.07f, 0.08f, 0.15f);
        }

        public float GetEmptyChance(int depth)
        {
            return Mathf.Clamp(0.15f - depth * 0.0005f, 0.05f, 0.15f);
        }

        public float GetIndestructibleChance(int depth)
        {
            return Mathf.Clamp(0.02f + depth * 0.0003f, 0.02f, 0.10f);
        }

        public float GetHardChance(int depth)
        {
            return Mathf.Clamp(0.05f + depth * 0.001f, 0.05f, 0.35f);
        }

        public float GetHardness(int depth)
        {
            return 2f + Mathf.Clamp(depth / 200f, 0f, 2f);
        }

        public int GetOreGold(int depth, float goldMultiplier = 0f)
        {
            var baseValue = 10f + depth * 0.5f;
            var randomScale = (float)GD.RandRange(0.8, 1.2);
            return Mathf.Max(1, Mathf.RoundToInt(baseValue * randomScale * (1f + goldMultiplier)));
        }

        public float GetBaseEquipmentValue(EquipmentCategory category, int depth)
        {
            var clampedDepth = Mathf.Max(0, depth);
            var depthRangeValue = category switch
            {
                EquipmentCategory.Pickaxe => GetDepthScaledValue(clampedDepth, 10f, 30f, 30f, 100f, 100f, 500f, 500f, 2000f, 2000f, 10000f),
                EquipmentCategory.Armor => GetDepthScaledValue(clampedDepth, 4f, 8f, 8f, 20f, 20f, 70f, 70f, 200f, 200f, 800f),
                EquipmentCategory.Accessory => GetDepthScaledValue(clampedDepth, 2f, 5f, 5f, 15f, 15f, 40f, 40f, 120f, 120f, 400f),
                EquipmentCategory.Boots => GetDepthScaledValue(clampedDepth, 0.04f, 0.08f, 0.08f, 0.15f, 0.15f, 0.30f, 0.30f, 0.45f, 0.45f, 0.80f),
                _ => 1f
            };

            return depthRangeValue;
        }

        private static float GetDepthScaledValue(
            int depth,
            float low0,
            float high0,
            float low1,
            float high1,
            float low2,
            float high2,
            float low3,
            float high3,
            float low4,
            float high4)
        {
            var random = (float)GD.RandRange(0.0, 1.0);
            return depth switch
            {
                <= 100 => Mathf.Lerp(low0, high0, random),
                <= 300 => Mathf.Lerp(low1, high1, random),
                <= 600 => Mathf.Lerp(low2, high2, random),
                <= 1000 => Mathf.Lerp(low3, high3, random),
                _ => Mathf.Lerp(low4, high4, random)
            };
        }
    }
}