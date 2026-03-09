using System.Linq;
using CursedBlood.Achievement;
using CursedBlood.Config;
using CursedBlood.Core;
using CursedBlood.Equipment;
using CursedBlood.Skill;
using Godot;

namespace CursedBlood.Player
{
    public enum LifePhase
    {
        Youth,
        Prime,
        Twilight
    }

    public sealed class PlayerStats
    {
        private float _invincibilityTimer;
        private float _dashTimer;
        private float _hpDrainRemainder;

        public string CharacterName { get; set; } = "深掘 アオイ";

        public bool IsMale { get; set; }

        public float BaseMaxLifespan { get; private set; } = 60f;

        public float MaxLifespan { get; private set; } = 60f;

        public float CurrentAge { get; private set; }

        public int BaseMaxHp { get; private set; } = 100;

        public int MaxHp { get; private set; } = 100;

        public int CurrentHp { get; private set; } = 100;

        public Vector2I GridPosition { get; set; } = new(3, 0);

        public int MaxDepth { get; set; }

        public int CurrentDepth => GridPosition.Y;

        public float BaseMoveSpeed { get; private set; } = 0.3f;

        public float BaseDigPower { get; private set; } = 10f;

        public int BlocksDug { get; private set; }

        public int EnemiesKilled { get; private set; }

        public int BossesKilled { get; private set; }

        public int MaxCombo { get; private set; }

        public int CurrentCombo { get; private set; }

        public float ComboTimer { get; private set; }

        public long Gold { get; private set; }

        public long LifetimeGold { get; private set; }

        public int Generation { get; set; } = 1;

        public float SkillGauge { get; private set; }

        public float LastRepaymentRate { get; set; }

        public long LastRepaymentAmount { get; set; }

        public bool LiberationBonusActive { get; set; }

        public float ExtendedLifespanBonus { get; private set; }

        public AchievementBonuses AchievementBonuses { get; private set; } = new();

        public Inventory Inventory { get; } = new();

        public EquipmentStats EquipmentTotals => Inventory.CalculateTotalStats();

        public bool HasCursedEquipment => Inventory.EnumerateAllItems().Any(item => item.Rarity == Rarity.Cursed);

        public bool IsInvincible => _invincibilityTimer > 0f || _dashTimer > 0f;

        public SkillType CurrentSkillType => EquipmentTotals.PreferredSkill;

        public float HumanAge => CurrentAge * 35f / MaxLifespan;

        public LifePhase Phase
        {
            get
            {
                if (CurrentAge <= 20f)
                {
                    return LifePhase.Youth;
                }

                if (CurrentAge <= 45f)
                {
                    return LifePhase.Prime;
                }

                return LifePhase.Twilight;
            }
        }

        public float PhaseMultiplier => Phase switch
        {
            LifePhase.Youth => AchievementBonuses.YouthMultiplierOverride > 0f ? AchievementBonuses.YouthMultiplierOverride : 0.6f,
            LifePhase.Prime => 1.0f,
            LifePhase.Twilight => AchievementBonuses.TwilightMultiplierOverride > 0f ? AchievementBonuses.TwilightMultiplierOverride : 0.7f,
            _ => 1.0f
        };

        public float EffectiveAllStatsMultiplier => Mathf.Max(0.2f, AchievementBonuses.AllStatsMultiplier);

        public float EffectiveDamageReduction => Mathf.Clamp(EquipmentTotals.DamageReduction + AchievementBonuses.DamageReductionBonus, 0f, 0.8f);

        public float EffectiveCritRate => Mathf.Clamp(EquipmentTotals.CritRate + AchievementBonuses.CritRateBonus, 0f, 0.95f);

        public float EffectiveCritDamageMultiplier => 1f + EquipmentTotals.CritDamage + AchievementBonuses.CritDamageBonus;

        public float EffectiveBossDamageMultiplier => 1f + EquipmentTotals.BossDamage + AchievementBonuses.BossDamageBonus;

        public float EffectiveDropRateBonus => EquipmentTotals.DropRateBonus + AchievementBonuses.DropRateBonus;

        public float EffectiveInheritanceRate => AchievementBonuses.InheritanceRateOverride > 0f ? AchievementBonuses.InheritanceRateOverride : 0.3f;

        public float EffectiveMoveSpeed
        {
            get
            {
                var speedMultiplier = Mathf.Max(0.2f, 1f + EquipmentTotals.MoveSpeedBonus + AchievementBonuses.MoveSpeedBonus - EquipmentTotals.SpeedPenalty);
                speedMultiplier *= EffectiveAllStatsMultiplier;
                if (_dashTimer > 0f)
                {
                    speedMultiplier *= 3f;
                }

                if (LiberationBonusActive)
                {
                    speedMultiplier *= 1.2f;
                }

                if (EquipmentTotals.LastSpurt && CurrentHp <= MaxHp * 0.3f)
                {
                    speedMultiplier *= 1.2f;
                }

                return Mathf.Max(0.08f, BaseMoveSpeed / Mathf.Max(0.1f, PhaseMultiplier * speedMultiplier));
            }
        }

        public float EffectiveDigPower
        {
            get
            {
                var digPower = BaseDigPower + EquipmentTotals.AttackPower;
                digPower *= EffectiveAllStatsMultiplier;
                digPower *= AchievementBonuses.DigPowerMultiplier;

                if (LiberationBonusActive)
                {
                    digPower *= 1.2f;
                }

                if (EquipmentTotals.LastSpurt && CurrentHp <= MaxHp * 0.3f)
                {
                    digPower *= 1.2f;
                }

                return digPower;
            }
        }

        public bool IsAlive => CurrentHp > 0 && CurrentAge < MaxLifespan;

        public void ApplyRunSetup(BalanceConfig config, int generation, string characterName, bool isMale, long startingGold, EquipmentData heirloom, float extendedLifespanBonus, bool liberationBonus, AchievementBonuses bonuses)
        {
            Generation = generation;
            CharacterName = characterName;
            IsMale = isMale;
            AchievementBonuses = bonuses ?? new AchievementBonuses();
            LiberationBonusActive = liberationBonus;
            ExtendedLifespanBonus = extendedLifespanBonus + AchievementBonuses.LifespanBonus;
            BaseMaxLifespan = config.LifespanSeconds;
            MaxLifespan = BaseMaxLifespan + ExtendedLifespanBonus;
            BaseMoveSpeed = config.BaseMoveSpeed;
            BaseDigPower = 10f;
            BaseMaxHp = 100;

            CurrentAge = 0f;
            GridPosition = new Vector2I(3, 0);
            MaxDepth = 0;
            BlocksDug = 0;
            EnemiesKilled = 0;
            BossesKilled = 0;
            MaxCombo = 0;
            CurrentCombo = 0;
            ComboTimer = 0f;
            SkillGauge = 0f;
            Gold = startingGold;
            LifetimeGold = startingGold;
            LastRepaymentRate = 0f;
            LastRepaymentAmount = 0L;
            _invincibilityTimer = 0f;
            _dashTimer = 0f;
            _hpDrainRemainder = 0f;
            Inventory.ResetForNextGeneration(heirloom);

            RefreshDerivedStats();
            CurrentHp = MaxHp;
        }

        public void RefreshDerivedStats()
        {
            MaxLifespan = BaseMaxLifespan + ExtendedLifespanBonus;
            MaxHp = BaseMaxHp + Mathf.RoundToInt(EquipmentTotals.MaxHpBonus) + AchievementBonuses.MaxHpBonus;
            MaxHp = Mathf.RoundToInt(MaxHp * EffectiveAllStatsMultiplier);
            if (LiberationBonusActive)
            {
                MaxHp = Mathf.RoundToInt(MaxHp * 1.2f);
            }

            CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
        }

        public void AdvanceTime(float delta)
        {
            CurrentAge += delta;

            if (ComboTimer > 0f)
            {
                ComboTimer = Mathf.Max(0f, ComboTimer - delta);
                if (ComboTimer <= 0f)
                {
                    CurrentCombo = 0;
                }
            }

            if (_invincibilityTimer > 0f)
            {
                _invincibilityTimer = Mathf.Max(0f, _invincibilityTimer - delta);
            }

            if (_dashTimer > 0f)
            {
                _dashTimer = Mathf.Max(0f, _dashTimer - delta);
            }

            var hpDrain = EquipmentTotals.HpDrainPerSecond * delta;
            if (hpDrain > 0f)
            {
                _hpDrainRemainder += hpDrain;
                while (_hpDrainRemainder >= 1f)
                {
                    _hpDrainRemainder -= 1f;
                    TakeDamage(1, ignoreInvincibility: true);
                }
            }
        }

        public void TakeDamage(int damage, bool ignoreInvincibility = false)
        {
            if (damage <= 0)
            {
                return;
            }

            if (IsInvincible && !ignoreInvincibility)
            {
                return;
            }

            var adjustedDamage = damage * (1f - EffectiveDamageReduction);
            adjustedDamage *= 1f + EquipmentTotals.DamagePenalty;
            adjustedDamage = Mathf.Max(1f, adjustedDamage - EquipmentTotals.DefensePower * 0.02f);

            CurrentHp = Mathf.Max(0, CurrentHp - Mathf.CeilToInt(adjustedDamage));
            if (EquipmentTotals.InvincibleOnHit > 0f)
            {
                _invincibilityTimer = Mathf.Max(_invincibilityTimer, EquipmentTotals.InvincibleOnHit);
            }
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public void SpendGold(long amount)
        {
            Gold = System.Math.Max(0L, Gold - amount);
        }

        public void AddGold(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var adjustedAmount = (long)System.MathF.Ceiling(amount * (1f + EquipmentTotals.GoldBonus + AchievementBonuses.GoldBonus - EquipmentTotals.GoldPenalty));
            Gold += adjustedAmount;
            LifetimeGold += adjustedAmount;
        }

        public void RegisterDig(float skillGaugeAmount)
        {
            BlocksDug++;
            AddSkillGauge(skillGaugeAmount);
        }

        public void RegisterEnemyKill(float skillGaugeAmount)
        {
            EnemiesKilled++;
            CurrentCombo++;
            MaxCombo = Mathf.Max(MaxCombo, CurrentCombo);
            ComboTimer = 3f + AchievementBonuses.ComboTimerBonus;
            AddSkillGauge(skillGaugeAmount);
        }

        public void RegisterBossKill()
        {
            BossesKilled++;
        }

        public void ResetCombo()
        {
            CurrentCombo = 0;
            ComboTimer = 0f;
        }

        public void AddSkillGauge(float amount)
        {
            SkillGauge = Mathf.Clamp(SkillGauge + amount, 0f, 100f);
        }

        public bool ConsumeSkill(float amount = 100f)
        {
            if (SkillGauge < amount)
            {
                return false;
            }

            SkillGauge -= amount;
            return true;
        }

        public void ActivateDash(float duration)
        {
            _dashTimer = Mathf.Max(_dashTimer, duration);
        }

        public float GetMovementDuration(CellType cellType, float hardnessMultiplier)
        {
            var baseDuration = EffectiveMoveSpeed;
            if (cellType == CellType.Empty)
            {
                return baseDuration * 0.5f;
            }

            var digSpeedMultiplier = 1f + AchievementBonuses.DigSpeedBonus;
            if (cellType == CellType.Hard)
            {
                digSpeedMultiplier += AchievementBonuses.HardBlockBonus;
            }

            return baseDuration * Mathf.Max(0.5f, hardnessMultiplier) / Mathf.Max(0.2f, digSpeedMultiplier);
        }

        public int CalculateAttackDamage(float multiplier = 1f, bool againstBoss = false)
        {
            var damage = EffectiveDigPower * multiplier;
            if (againstBoss)
            {
                damage *= EffectiveBossDamageMultiplier;
            }

            if (GD.Randf() < EffectiveCritRate)
            {
                damage *= EffectiveCritDamageMultiplier;
            }

            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        public long CalculateScore()
        {
            var comboBonus = 1f + MaxCombo / 100f;
            var repaymentBonus = 1f + LastRepaymentRate;
            var generationBonus = 1f + Generation * 0.05f;
            var achievementBonus = 1f + AchievementBonuses.ScoreBonus;
            return (long)(Mathf.Max(1, MaxDepth) * Mathf.Max(1, EnemiesKilled) * comboBonus * repaymentBonus * generationBonus * achievementBonus);
        }

        public void Reset()
        {
            CurrentAge = 0f;
            CurrentHp = MaxHp;
            GridPosition = new Vector2I(3, 0);
            MaxDepth = 0;
            BlocksDug = 0;
            EnemiesKilled = 0;
            BossesKilled = 0;
            MaxCombo = 0;
            CurrentCombo = 0;
            ComboTimer = 0f;
            Gold = 0;
            LifetimeGold = 0;
            SkillGauge = 0f;
            _invincibilityTimer = 0f;
            _dashTimer = 0f;
            _hpDrainRemainder = 0f;
        }
    }
}