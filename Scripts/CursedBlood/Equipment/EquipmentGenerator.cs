using System;
using System.Collections.Generic;
using System.Linq;
using CursedBlood.Config;
using CursedBlood.Skill;

namespace CursedBlood.Equipment
{
    public static class EquipmentGenerator
    {
        private static readonly Random Rng = new();

        private static readonly string[] Materials =
        {
            "鉄",
            "鋼",
            "黒曜",
            "深淵",
            "呪鉄",
            "星屑",
            "血晶"
        };

        private static readonly string[] CategoryNames =
        {
            "ツルハシ",
            "鎧",
            "護符",
            "靴"
        };

        private static readonly EffectType[] EffectPool =
        {
            EffectType.DigSpeed,
            EffectType.CritRate,
            EffectType.CritDamage,
            EffectType.HardBlockDamage,
            EffectType.BossDamage,
            EffectType.DamageReduction,
            EffectType.HpBonus,
            EffectType.InvincibleOnHit,
            EffectType.BulletSlowAura,
            EffectType.MoveSpeed,
            EffectType.DirectionChangeBoost,
            EffectType.GoldBonus,
            EffectType.DropRateBonus,
            EffectType.OreVisionRange,
            EffectType.DebtRepayBonus,
            EffectType.DoubleDig,
            EffectType.ChainExplosion,
            EffectType.GoldMagnet,
            EffectType.CurseAbsorb,
            EffectType.LastSpurt
        };

        public static EquipmentData Generate(int depth, BalanceConfig config, Rarity? forceRarity = null)
        {
            var category = (EquipmentCategory)Rng.Next(0, 4);
            var rarity = forceRarity ?? RollStandardRarity();
            var baseValue = config.GetBaseEquipmentValue(category, depth) * GetRarityMultiplier(rarity);
            var skill = category == EquipmentCategory.Pickaxe ? RollSkill() : SkillType.AreaBreak3x3;
            var effects = RollEffects(rarity);

            return new EquipmentData
            {
                Name = GenerateName(category, effects.Count),
                Category = category,
                Rarity = rarity,
                DropDepth = depth,
                BaseValue = baseValue,
                Effects = effects,
                Demerit = rarity == Rarity.Cursed ? RollDemerit() : null,
                SkillType = skill
            };
        }

        public static EquipmentData GenerateBossDrop(int depth, BalanceConfig config)
        {
            var rarityRoll = Rng.NextDouble();
            var rarity = rarityRoll switch
            {
                < 0.70 => Rarity.Rare,
                < 0.90 => Rarity.Epic,
                < 0.98 => Rarity.Legendary,
                _ => Rarity.Cursed
            };

            return Generate(depth, config, rarity);
        }

        public static Rarity RollStandardRarity()
        {
            var roll = Rng.NextDouble() * 100d;
            return roll switch
            {
                < 63.12 => Rarity.Common,
                < 88.12 => Rarity.Uncommon,
                < 98.12 => Rarity.Rare,
                < 99.62 => Rarity.Epic,
                < 99.92 => Rarity.Legendary,
                _ => Rarity.Cursed
            };
        }

        private static float GetRarityMultiplier(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 1f,
                Rarity.Uncommon => 3f,
                Rarity.Rare => 10f,
                Rarity.Epic => 50f,
                Rarity.Legendary => 500f,
                Rarity.Cursed => 5000f,
                _ => 1f
            };
        }

        private static int GetEffectCount(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => Rng.Next(0, 2),
                Rarity.Uncommon => Rng.Next(0, 3),
                Rarity.Rare => Rng.Next(1, 3),
                Rarity.Epic => Rng.Next(2, 4),
                Rarity.Legendary => Rng.Next(2, 4),
                Rarity.Cursed => 3,
                _ => 0
            };
        }

        private static List<EquipmentEffect> RollEffects(Rarity rarity)
        {
            var count = GetEffectCount(rarity);
            var chosen = EffectPool.OrderBy(_ => Rng.Next()).Take(count).ToList();
            var effects = new List<EquipmentEffect>(count);
            foreach (var effectType in chosen)
            {
                effects.Add(new EquipmentEffect
                {
                    Type = effectType,
                    Value = RollEffectValue(effectType, rarity)
                });
            }

            return effects;
        }

        private static float RollEffectValue(EffectType type, Rarity rarity)
        {
            var baseValue = type switch
            {
                EffectType.DigSpeed => 0.08f,
                EffectType.CritRate => 0.05f,
                EffectType.CritDamage => 0.20f,
                EffectType.HardBlockDamage => 0.15f,
                EffectType.BossDamage => 0.15f,
                EffectType.DamageReduction => 0.04f,
                EffectType.HpBonus => 8f,
                EffectType.InvincibleOnHit => 0.2f,
                EffectType.BulletSlowAura => 0.15f,
                EffectType.MoveSpeed => 0.08f,
                EffectType.DirectionChangeBoost => 0.12f,
                EffectType.GoldBonus => 0.12f,
                EffectType.DropRateBonus => 0.06f,
                EffectType.OreVisionRange => 1f,
                EffectType.DebtRepayBonus => 0.08f,
                EffectType.DoubleDig => 1f,
                EffectType.ChainExplosion => 1f,
                EffectType.GoldMagnet => 1f,
                EffectType.CurseAbsorb => 1f,
                EffectType.LastSpurt => 1f,
                _ => 0.1f
            };

            var multiplier = rarity switch
            {
                Rarity.Common => Rng.NextDouble() * 0.5 + 0.5,
                Rarity.Uncommon => Rng.NextDouble() * 0.5 + 1.0,
                Rarity.Rare => Rng.NextDouble() + 1.5,
                Rarity.Epic => Rng.NextDouble() * 1.5 + 2.5,
                Rarity.Legendary => Rng.NextDouble() * 4.0 + 4.0,
                Rarity.Cursed => Rng.NextDouble() * 7.0 + 8.0,
                _ => 1.0
            };

            return baseValue * (float)multiplier;
        }

        private static EquipmentEffect RollDemerit()
        {
            var demeritType = (DemeritType)Rng.Next(1, 5);
            var value = demeritType switch
            {
                DemeritType.HpDrain => 1f,
                DemeritType.GoldPenalty => 0.5f,
                DemeritType.SpeedPenalty => 0.2f,
                DemeritType.DamagePenalty => 0.3f,
                _ => 0f
            };

            return new EquipmentEffect
            {
                IsDemerit = true,
                DemeritType = demeritType,
                Value = value
            };
        }

        private static SkillType RollSkill()
        {
            return (SkillType)Rng.Next(0, 4);
        }

        private static string GenerateName(EquipmentCategory category, int effectCount)
        {
            var material = Materials[Rng.Next(Materials.Length)];
            var categoryName = CategoryNames[(int)category];
            return $"{material}の{categoryName}+{effectCount}";
        }
    }
}