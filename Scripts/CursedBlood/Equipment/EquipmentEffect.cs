using System.Collections.Generic;
using System.Linq;
using CursedBlood.Skill;

namespace CursedBlood.Equipment
{
    public enum EffectType
    {
        DigSpeed,
        CritRate,
        CritDamage,
        HardBlockDamage,
        BossDamage,
        DamageReduction,
        HpBonus,
        InvincibleOnHit,
        BulletSlowAura,
        MoveSpeed,
        DirectionChangeBoost,
        GoldBonus,
        DropRateBonus,
        OreVisionRange,
        DebtRepayBonus,
        DoubleDig,
        ChainExplosion,
        GoldMagnet,
        CurseAbsorb,
        LastSpurt
    }

    public enum DemeritType
    {
        None,
        HpDrain,
        GoldPenalty,
        SpeedPenalty,
        DamagePenalty
    }

    public sealed class EquipmentEffect
    {
        public EffectType Type { get; set; }

        public float Value { get; set; }

        public bool IsDemerit { get; set; }

        public DemeritType DemeritType { get; set; } = DemeritType.None;

        public EquipmentEffect Clone()
        {
            return new EquipmentEffect
            {
                Type = Type,
                Value = Value,
                IsDemerit = IsDemerit,
                DemeritType = DemeritType
            };
        }
    }

    public sealed class EquipmentStats
    {
        public float AttackPower { get; set; }

        public float DefensePower { get; set; }

        public float MoveSpeedBonus { get; set; }

        public float GoldBonus { get; set; }

        public float DropRateBonus { get; set; }

        public float DamageReduction { get; set; }

        public float MaxHpBonus { get; set; }

        public float DigSpeedBonus { get; set; }

        public float CritRate { get; set; }

        public float CritDamage { get; set; }

        public float HardBlockDamage { get; set; }

        public float BossDamage { get; set; }

        public float InvincibleOnHit { get; set; }

        public float BulletSlowAura { get; set; }

        public float DirectionChangeBoost { get; set; }

        public float OreVisionRange { get; set; }

        public float DebtRepayBonus { get; set; }

        public bool DoubleDig { get; set; }

        public bool ChainExplosion { get; set; }

        public bool GoldMagnet { get; set; }

        public bool CurseAbsorb { get; set; }

        public bool LastSpurt { get; set; }

        public float HpDrainPerSecond { get; set; }

        public float GoldPenalty { get; set; }

        public float SpeedPenalty { get; set; }

        public float DamagePenalty { get; set; }

        public SkillType PreferredSkill { get; set; } = SkillType.AreaBreak3x3;

        public void Merge(EquipmentStats other)
        {
            AttackPower += other.AttackPower;
            DefensePower += other.DefensePower;
            MoveSpeedBonus += other.MoveSpeedBonus;
            GoldBonus += other.GoldBonus;
            DropRateBonus += other.DropRateBonus;
            DamageReduction += other.DamageReduction;
            MaxHpBonus += other.MaxHpBonus;
            DigSpeedBonus += other.DigSpeedBonus;
            CritRate += other.CritRate;
            CritDamage += other.CritDamage;
            HardBlockDamage += other.HardBlockDamage;
            BossDamage += other.BossDamage;
            InvincibleOnHit += other.InvincibleOnHit;
            BulletSlowAura += other.BulletSlowAura;
            DirectionChangeBoost += other.DirectionChangeBoost;
            OreVisionRange += other.OreVisionRange;
            DebtRepayBonus += other.DebtRepayBonus;
            DoubleDig |= other.DoubleDig;
            ChainExplosion |= other.ChainExplosion;
            GoldMagnet |= other.GoldMagnet;
            CurseAbsorb |= other.CurseAbsorb;
            LastSpurt |= other.LastSpurt;
            HpDrainPerSecond += other.HpDrainPerSecond;
            GoldPenalty += other.GoldPenalty;
            SpeedPenalty += other.SpeedPenalty;
            DamagePenalty += other.DamagePenalty;
            PreferredSkill = other.AttackPower >= AttackPower ? other.PreferredSkill : PreferredSkill;
        }
    }

    public static class EquipmentEffectResolver
    {
        public static EquipmentStats Calculate(IEnumerable<EquipmentData> items)
        {
            var totals = new EquipmentStats();
            foreach (var item in items.Where(item => item != null))
            {
                ApplyBaseValue(totals, item);

                foreach (var effect in item.Effects)
                {
                    ApplyEffect(totals, effect);
                }

                if (item.Demerit != null)
                {
                    ApplyDemerit(totals, item.Demerit);
                }
            }

            totals.DamageReduction = System.Math.Clamp(totals.DamageReduction, 0f, 0.8f);
            return totals;
        }

        private static void ApplyBaseValue(EquipmentStats totals, EquipmentData item)
        {
            switch (item.Category)
            {
                case EquipmentCategory.Pickaxe:
                    totals.AttackPower += item.BaseValue;
                    totals.DigSpeedBonus += item.BaseValue * 0.0025f;
                    totals.PreferredSkill = item.SkillType;
                    break;
                case EquipmentCategory.Armor:
                    totals.DefensePower += item.BaseValue;
                    totals.DamageReduction += item.BaseValue * 0.001f;
                    totals.MaxHpBonus += item.BaseValue * 0.25f;
                    break;
                case EquipmentCategory.Accessory:
                    totals.GoldBonus += item.BaseValue * 0.01f;
                    totals.DropRateBonus += item.BaseValue * 0.005f;
                    break;
                case EquipmentCategory.Boots:
                    totals.MoveSpeedBonus += item.BaseValue;
                    break;
            }
        }

        private static void ApplyEffect(EquipmentStats totals, EquipmentEffect effect)
        {
            switch (effect.Type)
            {
                case EffectType.DigSpeed:
                    totals.DigSpeedBonus += effect.Value;
                    break;
                case EffectType.CritRate:
                    totals.CritRate += effect.Value;
                    break;
                case EffectType.CritDamage:
                    totals.CritDamage += effect.Value;
                    break;
                case EffectType.HardBlockDamage:
                    totals.HardBlockDamage += effect.Value;
                    break;
                case EffectType.BossDamage:
                    totals.BossDamage += effect.Value;
                    break;
                case EffectType.DamageReduction:
                    totals.DamageReduction += effect.Value;
                    break;
                case EffectType.HpBonus:
                    totals.MaxHpBonus += effect.Value;
                    break;
                case EffectType.InvincibleOnHit:
                    totals.InvincibleOnHit += effect.Value;
                    break;
                case EffectType.BulletSlowAura:
                    totals.BulletSlowAura += effect.Value;
                    break;
                case EffectType.MoveSpeed:
                    totals.MoveSpeedBonus += effect.Value;
                    break;
                case EffectType.DirectionChangeBoost:
                    totals.DirectionChangeBoost += effect.Value;
                    break;
                case EffectType.GoldBonus:
                    totals.GoldBonus += effect.Value;
                    break;
                case EffectType.DropRateBonus:
                    totals.DropRateBonus += effect.Value;
                    break;
                case EffectType.OreVisionRange:
                    totals.OreVisionRange += effect.Value;
                    break;
                case EffectType.DebtRepayBonus:
                    totals.DebtRepayBonus += effect.Value;
                    break;
                case EffectType.DoubleDig:
                    totals.DoubleDig = true;
                    break;
                case EffectType.ChainExplosion:
                    totals.ChainExplosion = true;
                    break;
                case EffectType.GoldMagnet:
                    totals.GoldMagnet = true;
                    break;
                case EffectType.CurseAbsorb:
                    totals.CurseAbsorb = true;
                    break;
                case EffectType.LastSpurt:
                    totals.LastSpurt = true;
                    break;
            }
        }

        private static void ApplyDemerit(EquipmentStats totals, EquipmentEffect effect)
        {
            switch (effect.DemeritType)
            {
                case DemeritType.HpDrain:
                    totals.HpDrainPerSecond += effect.Value;
                    break;
                case DemeritType.GoldPenalty:
                    totals.GoldPenalty += effect.Value;
                    break;
                case DemeritType.SpeedPenalty:
                    totals.SpeedPenalty += effect.Value;
                    break;
                case DemeritType.DamagePenalty:
                    totals.DamagePenalty += effect.Value;
                    break;
            }
        }
    }
}