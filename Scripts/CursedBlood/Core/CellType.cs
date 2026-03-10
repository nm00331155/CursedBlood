using Godot;

namespace CursedBlood.Core
{
    public enum CellType : byte
    {
        Empty = 0,
        Dirt = 1,
        Stone = 2,
        HardRock = 3,
        Ore = 4,
        Bedrock = 5,
        Enemy = 6,
        Boss = 7,
        RecoveryPoint = 8,
        Item = 9
    }

    public static class CellTypeUtil
    {
        public static float GetHardness(CellType type)
        {
            return type switch
            {
                CellType.Empty => 1f,
                CellType.Dirt => 1f,
                CellType.Stone => 1.35f,
                CellType.HardRock => 1.85f,
                CellType.Ore => 1.15f,
                CellType.RecoveryPoint => 1f,
                CellType.Item => 1f,
                CellType.Bedrock => float.MaxValue,
                CellType.Enemy => 1f,
                CellType.Boss => 1f,
                _ => 1f
            };
        }

        public static bool IsDiggable(CellType type)
        {
            return type != CellType.Bedrock && type != CellType.Enemy && type != CellType.Boss;
        }

        public static bool IsPassable(CellType type)
        {
            return type is CellType.Empty or CellType.RecoveryPoint or CellType.Item;
        }

        public static bool RequiresDig(CellType type)
        {
            return !IsPassable(type) && IsDiggable(type);
        }

        public static bool IsHardDig(CellType type)
        {
            return GetHardness(type) > 1f;
        }

        public static string GetName(CellType type)
        {
            return type switch
            {
                CellType.Empty => "Empty",
                CellType.Dirt => "Dirt",
                CellType.Stone => "Stone",
                CellType.HardRock => "HardRock",
                CellType.Ore => "Ore",
                CellType.Bedrock => "Bedrock",
                CellType.Enemy => "Enemy",
                CellType.Boss => "Boss",
                CellType.RecoveryPoint => "RecoveryPoint",
                CellType.Item => "Item",
                _ => "Unknown"
            };
        }

        public static Color GetColor(CellType type, int depthTier)
        {
            depthTier = Mathf.Clamp(depthTier, 0, 4);
            var shade = depthTier * 0.06f;

            return type switch
            {
                CellType.Empty => Shade(new Color(0.03f, 0.05f, 0.08f), shade * 0.65f),
                CellType.Dirt => Shade(new Color(0.76f, 0.54f, 0.28f), shade * 0.82f),
                CellType.Stone => Shade(new Color(0.60f, 0.71f, 0.80f), shade),
                CellType.HardRock => Shade(new Color(0.23f, 0.33f, 0.42f), shade * 0.70f),
                CellType.Ore => Shade(new Color(0.98f, 0.84f, 0.28f), shade * 0.32f),
                CellType.Bedrock => Shade(new Color(0.08f, 0.06f, 0.10f), shade * 0.2f),
                CellType.Enemy => Shade(new Color(0.92f, 0.33f, 0.31f), shade * 0.25f),
                CellType.Boss => Shade(new Color(0.74f, 0.18f, 0.22f), shade * 0.25f),
                CellType.RecoveryPoint => Shade(new Color(0.32f, 0.95f, 0.86f), shade * 0.18f),
                CellType.Item => Shade(new Color(1.00f, 0.63f, 0.24f), shade * 0.18f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };
        }

        private static Color Shade(Color color, float amount)
        {
            return color.Lerp(new Color(0.02f, 0.03f, 0.05f), Mathf.Clamp(amount, 0f, 0.32f));
        }
    }
}