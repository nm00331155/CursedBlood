using CursedBlood.Core;
using CursedBlood.Enemy;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Skill
{
    public static class SkillEffects
    {
        public static void LinearPierce(GridManager grid, PlayerStats stats, EnemyManager enemyManager, BossController bossController, Vector2I direction)
        {
            for (var step = 1; step <= 5; step++)
            {
                var position = stats.GridPosition + direction * step;
                var cell = grid.GetCell(position.X, position.Y);
                if (cell == null)
                {
                    break;
                }

                if (cell.HasBoss)
                {
                    bossController?.DealDirectDamage(stats.CalculateAttackDamage(1.25f, againstBoss: true));
                    continue;
                }

                if (cell.HasEnemy)
                {
                    enemyManager?.ResolveEnemyEncounter(cell);
                }

                if (cell.IsDiggable)
                {
                    cell.Dig();
                }
            }

            grid.QueueRefresh();
        }

        public static void AreaBreak(GridManager grid, PlayerStats stats, EnemyManager enemyManager, BossController bossController)
        {
            for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    var position = stats.GridPosition + new Vector2I(columnOffset, rowOffset);
                    var cell = grid.GetCell(position.X, position.Y);
                    if (cell == null)
                    {
                        continue;
                    }

                    if (cell.HasEnemy)
                    {
                        enemyManager?.ResolveEnemyEncounter(cell);
                    }

                    if (cell.HasBoss)
                    {
                        bossController?.DealDirectDamage(stats.CalculateAttackDamage(againstBoss: true));
                        continue;
                    }

                    if (cell.IsDiggable)
                    {
                        cell.Dig();
                    }
                }
            }

            grid.QueueRefresh();
        }

        public static void ScreenAttack(EnemyManager enemyManager, BossController bossController)
        {
            if (enemyManager?.Grid != null)
            {
                foreach (var cell in enemyManager.Grid.EnumerateVisibleCells())
                {
                    if (cell.HasEnemy)
                    {
                        enemyManager.ResolveEnemyEncounter(cell);
                    }
                }
            }

            bossController?.DealDirectDamage(200);
        }
    }
}