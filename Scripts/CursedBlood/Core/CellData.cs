using CursedBlood.Enemy;
using CursedBlood.Equipment;
using Godot;

namespace CursedBlood.Core
{
    public enum CellType
    {
        Empty,
        Normal,
        Hard,
        Ore,
        Enemy,
        Indestructible,
        Boss
    }

    public sealed class CellData
    {
        public CellData(CellType type, float hardness, Vector2I gridPosition)
        {
            Type = type;
            Hardness = hardness;
            GridPosition = gridPosition;
        }

        public CellType Type { get; private set; }

        public float Hardness { get; private set; }

        public Vector2I GridPosition { get; }

        public EnemyData Enemy { get; private set; }

        public BossCellData BossCell { get; private set; }

        public DroppedItem DroppedItem { get; private set; }

        public int OreValue { get; private set; }

        public bool IsDiggable => Type != CellType.Indestructible;

        public bool IsEmpty => Type == CellType.Empty;

        public bool HasEnemy => Enemy != null;

        public bool HasBoss => BossCell != null;

        public bool HasDrop => DroppedItem != null;

        public void SetType(CellType type, float hardness)
        {
            Type = type;
            Hardness = hardness;

            if (type != CellType.Enemy)
            {
                Enemy = null;
            }

            if (type != CellType.Boss)
            {
                BossCell = null;
            }

            if (type != CellType.Ore)
            {
                OreValue = 0;
            }
        }

        public void SetEnemy(EnemyData enemy, float hardness)
        {
            Type = CellType.Enemy;
            Hardness = hardness;
            Enemy = enemy;
            BossCell = null;
            OreValue = 0;
        }

        public void ClearEnemy()
        {
            Enemy = null;
            if (Type == CellType.Enemy)
            {
                Type = CellType.Empty;
                Hardness = 0f;
            }
        }

        public void SetBoss(BossCellData bossCellData)
        {
            Type = CellType.Boss;
            Hardness = 999f;
            BossCell = bossCellData;
            Enemy = null;
            OreValue = 0;
        }

        public void ClearBoss()
        {
            BossCell = null;
            if (Type == CellType.Boss)
            {
                Type = CellType.Empty;
                Hardness = 0f;
            }
        }

        public void SetOre(int oreValue)
        {
            Type = CellType.Ore;
            Hardness = 1f;
            OreValue = oreValue;
            Enemy = null;
            BossCell = null;
        }

        public void ClearOre()
        {
            OreValue = 0;
            if (Type == CellType.Ore)
            {
                Type = CellType.Empty;
                Hardness = 0f;
            }
        }

        public void SetDroppedItem(DroppedItem droppedItem)
        {
            DroppedItem = droppedItem;
        }

        public DroppedItem TakeDroppedItem()
        {
            var item = DroppedItem;
            DroppedItem = null;
            return item;
        }

        public void Dig()
        {
            if (!IsDiggable || IsEmpty)
            {
                return;
            }

            Type = CellType.Empty;
            Hardness = 0f;
            Enemy = null;
            BossCell = null;
            OreValue = 0;
        }
    }
}