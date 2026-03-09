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
                CellType.Stone => 2f,
                CellType.HardRock => 4f,
                CellType.Ore => 1.5f,
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
            var shade = depthTier * 0.08f;

            return type switch
            {
                CellType.Empty => new Color(0.08f, 0.08f, 0.10f),
                CellType.Dirt => Shade(new Color(0.53f, 0.34f, 0.18f), shade),
                CellType.Stone => Shade(new Color(0.58f, 0.61f, 0.66f), shade),
                CellType.HardRock => Shade(new Color(0.20f, 0.24f, 0.29f), shade),
                CellType.Ore => Shade(new Color(0.90f, 0.78f, 0.24f), shade * 0.5f),
                CellType.Bedrock => Shade(new Color(0.02f, 0.02f, 0.03f), shade * 0.2f),
                CellType.Enemy => Shade(new Color(0.78f, 0.22f, 0.22f), shade * 0.3f),
                CellType.Boss => Shade(new Color(0.62f, 0.16f, 0.18f), shade * 0.3f),
                CellType.RecoveryPoint => Shade(new Color(0.22f, 0.88f, 0.82f), shade * 0.2f),
                CellType.Item => Shade(new Color(0.88f, 0.52f, 0.24f), shade * 0.2f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };
        }

        private static Color Shade(Color color, float amount)
        {
            return color.Lerp(new Color(0.04f, 0.04f, 0.05f), Mathf.Clamp(amount, 0f, 0.35f));
        }
    }
}