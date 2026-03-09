using Godot;

namespace CursedBlood.Enemy
{
    public enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
        Phase4
    }

    public sealed class BossCellData
    {
        public Vector2I CenterPosition { get; set; }

        public int Depth { get; set; }

        public bool IsDemonLord { get; set; }
    }

    public sealed class BossData
    {
        public int Depth { get; set; }

        public int MaxHp { get; set; }

        public int CurrentHp { get; set; }

        public Vector2I CenterPosition { get; set; }

        public int DepthTier { get; set; }

        public bool IsDemonLord { get; set; }

        public bool IsDefeated { get; set; }

        public bool DropGranted { get; set; }

        public float AttackTimer { get; set; }

        public int WarningColumn { get; set; } = -1;

        public float WarningTimer { get; set; }

        public float HealthRatio => MaxHp <= 0 ? 0f : CurrentHp / (float)MaxHp;

        public BossPhase CurrentPhase
        {
            get
            {
                if (IsDemonLord)
                {
                    return HealthRatio switch
                    {
                        > 0.70f => BossPhase.Phase1,
                        > 0.40f => BossPhase.Phase2,
                        > 0.10f => BossPhase.Phase3,
                        _ => BossPhase.Phase4
                    };
                }

                return HealthRatio switch
                {
                    > 0.60f => BossPhase.Phase1,
                    > 0.30f => BossPhase.Phase2,
                    _ => BossPhase.Phase3
                };
            }
        }

        public void TakeDamage(int damage)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            if (CurrentHp <= 0)
            {
                IsDefeated = true;
            }
        }
    }
}