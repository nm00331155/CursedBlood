namespace CursedBlood.Achievement
{
    public enum AchievementCategory
    {
        Digging,
        Combat,
        Collection,
        Generation
    }

    public enum CounterType
    {
        OresBroken,
        HardBlocksBroken,
        EnemiesKilled,
        GuardBlocks,
        EquipmentFound,
        RarePlusFound,
        LegendaryFound,
        CursedFound,
        GoldInherited
    }

    public sealed class AchievementBonuses
    {
        public float DigPowerMultiplier { get; set; } = 1f;

        public float AllStatsMultiplier { get; set; } = 1f;

        public float DigSpeedBonus { get; set; }

        public float MoveSpeedBonus { get; set; }

        public float BossDamageBonus { get; set; }

        public float DamageReductionBonus { get; set; }

        public float GoldBonus { get; set; }

        public float DropRateBonus { get; set; }

        public float CurseResearchBonus { get; set; }

        public float CritRateBonus { get; set; }

        public float CritDamageBonus { get; set; }

        public float HardBlockBonus { get; set; }

        public float ComboTimerBonus { get; set; }

        public float InheritanceRateOverride { get; set; }

        public float YouthMultiplierOverride { get; set; }

        public float TwilightMultiplierOverride { get; set; }

        public float ScoreBonus { get; set; }

        public float LifespanBonus { get; set; }

        public int MaxHpBonus { get; set; }

        public int OreVisionBonus { get; set; }

        public void Merge(AchievementBonuses other)
        {
            if (other == null)
            {
                return;
            }

            DigPowerMultiplier *= other.DigPowerMultiplier;
            AllStatsMultiplier *= other.AllStatsMultiplier;
            DigSpeedBonus += other.DigSpeedBonus;
            MoveSpeedBonus += other.MoveSpeedBonus;
            BossDamageBonus += other.BossDamageBonus;
            DamageReductionBonus += other.DamageReductionBonus;
            GoldBonus += other.GoldBonus;
            DropRateBonus += other.DropRateBonus;
            CurseResearchBonus += other.CurseResearchBonus;
            CritRateBonus += other.CritRateBonus;
            CritDamageBonus += other.CritDamageBonus;
            HardBlockBonus += other.HardBlockBonus;
            ComboTimerBonus += other.ComboTimerBonus;
            ScoreBonus += other.ScoreBonus;
            LifespanBonus += other.LifespanBonus;
            MaxHpBonus += other.MaxHpBonus;
            OreVisionBonus += other.OreVisionBonus;

            if (other.InheritanceRateOverride > 0f)
            {
                InheritanceRateOverride = InheritanceRateOverride > 0f
                    ? System.Math.Max(InheritanceRateOverride, other.InheritanceRateOverride)
                    : other.InheritanceRateOverride;
            }

            if (other.YouthMultiplierOverride > 0f)
            {
                YouthMultiplierOverride = System.Math.Max(YouthMultiplierOverride, other.YouthMultiplierOverride);
            }

            if (other.TwilightMultiplierOverride > 0f)
            {
                TwilightMultiplierOverride = System.Math.Max(TwilightMultiplierOverride, other.TwilightMultiplierOverride);
            }
        }
    }

    public sealed class AchievementCounters
    {
        public long TotalOresBroken { get; set; }

        public long TotalHardBlocksBroken { get; set; }

        public long TotalEnemiesKilled { get; set; }

        public long TotalGuardBlocks { get; set; }

        public long TotalEquipmentFound { get; set; }

        public long TotalRarePlusFound { get; set; }

        public long TotalLegendaryFound { get; set; }

        public long TotalCursedFound { get; set; }

        public long TotalGoldInherited { get; set; }
    }

    public sealed class AchievementEntry
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string PassiveDescription { get; set; } = string.Empty;

        public AchievementCategory Category { get; set; }

        public bool Unlocked { get; set; }

        public float Progress { get; set; }

        public AchievementBonuses Bonus { get; set; } = new();
    }
}