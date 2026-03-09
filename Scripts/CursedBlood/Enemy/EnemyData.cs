using Godot;

namespace CursedBlood.Enemy
{
    public enum EnemyType
    {
        Slime,
        Shooter,
        Spreader,
        Bomber,
        Collector
    }

    public sealed class EnemyData
    {
        public EnemyType Type { get; set; }

        public int Hp { get; set; } = 1;

        public float AttackInterval { get; set; }

        public float AttackTimer { get; set; }

        public bool IsActive { get; set; }

        public int BulletDamage { get; set; }

        public float FuseTimer { get; set; }

        public bool FuseStarted { get; set; }

        public float MoveTimer { get; set; }

        public bool IsDebtCollector => Type == EnemyType.Collector;

        public static EnemyData Create(EnemyType type)
        {
            return type switch
            {
                EnemyType.Slime => new EnemyData { Type = type, AttackInterval = 0f, BulletDamage = 0 },
                EnemyType.Shooter => new EnemyData { Type = type, AttackInterval = 2f, BulletDamage = 10 },
                EnemyType.Spreader => new EnemyData { Type = type, AttackInterval = 3f, BulletDamage = 8 },
                EnemyType.Bomber => new EnemyData { Type = type, AttackInterval = 0f, BulletDamage = 30, FuseTimer = 3f },
                EnemyType.Collector => new EnemyData { Type = type, AttackInterval = 0f, BulletDamage = 15 },
                _ => new EnemyData { Type = EnemyType.Slime }
            };
        }

        public Color GetColor(int depthTier)
        {
            if (Type == EnemyType.Collector)
            {
                return new Color(0.85f, 0.12f, 0.12f);
            }

            return depthTier switch
            {
                0 => new Color(0.25f, 0.85f, 0.40f),
                1 => new Color(0.25f, 0.55f, 0.95f),
                2 => new Color(0.65f, 0.35f, 0.90f),
                _ => new Color(0.95f, 0.30f, 0.25f)
            };
        }

        public string GetGlyph()
        {
            return Type switch
            {
                EnemyType.Slime => "S",
                EnemyType.Shooter => "!",
                EnemyType.Spreader => "+",
                EnemyType.Bomber => "*",
                EnemyType.Collector => "$",
                _ => "?"
            };
        }
    }
}