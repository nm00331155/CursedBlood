using Godot;

namespace CursedBlood.Enemy
{
    public enum EnemyType
    {
        ThornMite,
        GasLeech
    }

    public sealed class EnemyData
    {
        public EnemyData(EnemyType type, string displayName, int contactDamage, float oxygenPenaltySeconds, float moveSlowdownMultiplier, float digSlowdownMultiplier, float debuffDurationSeconds, string statusLabel)
        {
            Type = type;
            DisplayName = displayName;
            ContactDamage = contactDamage;
            OxygenPenaltySeconds = oxygenPenaltySeconds;
            MoveSlowdownMultiplier = moveSlowdownMultiplier;
            DigSlowdownMultiplier = digSlowdownMultiplier;
            DebuffDurationSeconds = debuffDurationSeconds;
            StatusLabel = statusLabel;
        }

        public EnemyType Type { get; }

        public string DisplayName { get; }

        public int ContactDamage { get; }

        public float OxygenPenaltySeconds { get; }

        public float MoveSlowdownMultiplier { get; }

        public float DigSlowdownMultiplier { get; }

        public float DebuffDurationSeconds { get; }

        public string StatusLabel { get; }

        public static EnemyData Create(EnemyType type)
        {
            return type switch
            {
                EnemyType.GasLeech => new EnemyData(EnemyType.GasLeech, "瘴気ヒル", 8, 2.6f, 1.10f, 1.34f, 3.4f, "瘴気で掘削低下"),
                _ => new EnemyData(EnemyType.ThornMite, "刺胞虫", 14, 0.8f, 1.28f, 1.10f, 2.6f, "刺胞虫で減速")
            };
        }
    }

    public sealed class EnemyState
    {
        public EnemyState(int id, EnemyType type, Vector2I cell, float moveCooldown, float contactCooldown)
        {
            Id = id;
            Type = type;
            Cell = cell;
            MoveCooldown = moveCooldown;
            ContactCooldown = contactCooldown;
        }

        public int Id { get; }

        public EnemyType Type { get; }

        public Vector2I Cell { get; set; }

        public float MoveCooldown { get; set; }

        public float ContactCooldown { get; set; }
    }

    public readonly record struct EnemyDangerReading(bool HasDanger, Vector2I Hotspot, int NearbyCount, float Pressure)
    {
        public static readonly EnemyDangerReading None = new(false, Vector2I.Zero, 0, 0f);
    }
}