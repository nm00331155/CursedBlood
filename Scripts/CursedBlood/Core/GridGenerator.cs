using System;
using CursedBlood.Enemy;
using Godot;

namespace CursedBlood.Core
{
    public static class GridGenerator
    {
        private static readonly Random Rng = new();

        public const int Columns = 7;

        public static CellData[] GenerateRow(int rowIndex)
        {
            return GenerateRow(rowIndex, new GridGenerationContext());
        }

        public static CellData[] GenerateRow(int rowIndex, GridGenerationContext context)
        {
            context ??= new GridGenerationContext();

            if (TryGenerateSpecialRow(rowIndex, context, out var specialRow))
            {
                return specialRow;
            }

            var row = new CellData[Columns];

            for (var column = 0; column < Columns; column++)
            {
                if ((column == 0 || column == Columns - 1) && Rng.NextDouble() < 0.15)
                {
                    row[column] = new CellData(CellType.Indestructible, 0f, new Vector2I(column, rowIndex));
                    continue;
                }

                var roll = RollCellType(rowIndex);
                row[column] = new CellData(roll.type, roll.hardness, new Vector2I(column, rowIndex));
            }

            PlaceOresAndEnemies(row, rowIndex, context);
            EnsurePassable(row, rowIndex);
            PlaceCollector(row, rowIndex, context);
            return row;
        }

        public static CellData[] GenerateStartRow(int rowIndex)
        {
            return GenerateStartRow(rowIndex, new GridGenerationContext());
        }

        public static CellData[] GenerateStartRow(int rowIndex, GridGenerationContext context)
        {
            var row = new CellData[Columns];

            for (var column = 0; column < Columns; column++)
            {
                row[column] = new CellData(CellType.Empty, 0f, new Vector2I(column, rowIndex));
            }

            return row;
        }

        public static int GetDepthTier(int depth)
        {
            return depth switch
            {
                < 100 => 0,
                < 300 => 1,
                < 600 => 2,
                _ => 3
            };
        }

        private static (CellType type, float hardness) RollCellType(int depth)
        {
            var roll = Rng.NextDouble();
            var hardChance = Math.Min(0.05 + depth * 0.001, 0.35);
            var indestructibleChance = Math.Min(0.02 + depth * 0.0003, 0.10);
            var emptyChance = Math.Max(0.15 - depth * 0.0005, 0.05);

            if (roll < emptyChance)
            {
                return (CellType.Empty, 0f);
            }

            if (roll < emptyChance + indestructibleChance)
            {
                return (CellType.Indestructible, 0f);
            }

            if (roll < emptyChance + indestructibleChance + hardChance)
            {
                var hardness = 2f + (float)Math.Min(depth / 200.0, 2.0);
                return (CellType.Hard, hardness);
            }

            return (CellType.Normal, 1f);
        }

        private static void EnsurePassable(CellData[] row, int rowIndex)
        {
            foreach (var cell in row)
            {
                if (cell.IsDiggable)
                {
                    return;
                }
            }

            var midpoint = Columns / 2;
            row[midpoint] = new CellData(CellType.Normal, 1f, new Vector2I(midpoint, rowIndex));
        }

        private static void PlaceOresAndEnemies(CellData[] row, int rowIndex, GridGenerationContext context)
        {
            var oreChance = context.BalanceConfig.GetOreSpawnChance(rowIndex);
            var enemyChance = context.BalanceConfig.GetEnemySpawnChance(rowIndex);

            for (var column = 0; column < Columns; column++)
            {
                var cell = row[column];
                if (!cell.IsDiggable || cell.Type == CellType.Empty)
                {
                    continue;
                }

                var roll = Rng.NextDouble();
                if (roll < oreChance)
                {
                    cell.SetOre(context.BalanceConfig.GetOreGold(rowIndex));
                    continue;
                }

                if (roll >= oreChance + enemyChance)
                {
                    continue;
                }

                var enemyType = RollEnemyType(rowIndex);
                cell.SetEnemy(EnemyData.Create(enemyType), GetEnemyHardness(enemyType));
            }
        }

        private static EnemyType RollEnemyType(int depth)
        {
            if (depth < 50)
            {
                return EnemyType.Slime;
            }

            if (depth < 150)
            {
                return Rng.NextDouble() < 0.75 ? EnemyType.Slime : EnemyType.Shooter;
            }

            if (depth < 300)
            {
                var roll = Rng.NextDouble();
                return roll switch
                {
                    < 0.45 => EnemyType.Slime,
                    < 0.80 => EnemyType.Shooter,
                    _ => EnemyType.Spreader
                };
            }

            var lateRoll = Rng.NextDouble();
            return lateRoll switch
            {
                < 0.25 => EnemyType.Slime,
                < 0.55 => EnemyType.Shooter,
                < 0.80 => EnemyType.Spreader,
                _ => EnemyType.Bomber
            };
        }

        private static float GetEnemyHardness(EnemyType enemyType)
        {
            return enemyType switch
            {
                EnemyType.Slime => 2f,
                EnemyType.Shooter => 3f,
                EnemyType.Spreader => 4f,
                EnemyType.Bomber => 2f,
                EnemyType.Collector => 1.2f,
                _ => 1f
            };
        }

        private static void PlaceCollector(CellData[] row, int rowIndex, GridGenerationContext context)
        {
            if (context.CollectorSpawnMultiplier <= 0f)
            {
                return;
            }

            var spawnChance = Math.Min(0.01 * context.CollectorSpawnMultiplier, 0.08);
            if (Rng.NextDouble() >= spawnChance)
            {
                return;
            }

            var selectedIndex = -1;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var candidateIndex = Rng.Next(1, Columns - 1);
                var cell = row[candidateIndex];
                if (cell.IsDiggable && (cell.Type == CellType.Empty || cell.Type == CellType.Normal || cell.Type == CellType.Hard))
                {
                    selectedIndex = candidateIndex;
                    break;
                }
            }

            if (selectedIndex >= 0)
            {
                row[selectedIndex].SetEnemy(EnemyData.Create(EnemyType.Collector), 1.2f);
            }
        }

        private static bool TryGenerateSpecialRow(int rowIndex, GridGenerationContext context, out CellData[] row)
        {
            row = null;

            if (context.EnableDemonLord && Math.Abs(rowIndex - 9999) <= 5)
            {
                row = GenerateDemonLordArenaRow(rowIndex);
                return true;
            }

            var bossCenter = rowIndex / 100 * 100;
            if (!context.EnableBosses || bossCenter < 100 || Math.Abs(rowIndex - bossCenter) > 3 || bossCenter == 9900)
            {
                return false;
            }

            row = GenerateBossArenaRow(rowIndex, bossCenter);
            return true;
        }

        private static CellData[] GenerateBossArenaRow(int rowIndex, int bossCenter)
        {
            var row = CreateEmptyRow(rowIndex);
            if (Math.Abs(rowIndex - bossCenter) > 1)
            {
                return row;
            }

            for (var column = 2; column <= 4; column++)
            {
                row[column].SetBoss(new BossCellData
                {
                    CenterPosition = new Vector2I(3, bossCenter),
                    Depth = bossCenter,
                    IsDemonLord = false
                });
            }

            return row;
        }

        private static CellData[] GenerateDemonLordArenaRow(int rowIndex)
        {
            var row = CreateEmptyRow(rowIndex);
            if (Math.Abs(rowIndex - 9999) > 2)
            {
                return row;
            }

            for (var column = 1; column <= 5; column++)
            {
                row[column].SetBoss(new BossCellData
                {
                    CenterPosition = new Vector2I(3, 9999),
                    Depth = 9999,
                    IsDemonLord = true
                });
            }

            return row;
        }

        private static CellData[] CreateEmptyRow(int rowIndex)
        {
            var row = new CellData[Columns];
            for (var column = 0; column < Columns; column++)
            {
                row[column] = new CellData(CellType.Empty, 0f, new Vector2I(column, rowIndex));
            }

            return row;
        }
    }
}