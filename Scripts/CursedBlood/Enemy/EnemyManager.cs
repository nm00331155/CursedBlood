using System;
using System.Linq;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Enemy
{
    public partial class EnemyManager : Node2D
    {
        public GridManager Grid { get; set; }

        public PlayerStats Stats { get; set; }

        public BulletManager BulletManager { get; set; }

        public bool SimulationEnabled { get; set; } = true;

        public event Action<CellData, EnemyData> EnemyDefeated;

        public override void _Process(double delta)
        {
            if (!SimulationEnabled || Grid == null || Stats == null || BulletManager == null)
            {
                return;
            }

            var enemyCells = Grid.EnumerateVisibleCells().Where(cell => cell.HasEnemy).ToList();
            foreach (var cell in enemyCells)
            {
                ProcessEnemy(cell, (float)delta);
            }
        }

        public bool ResolveEnemyEncounter(CellData cell)
        {
            if (cell?.Enemy == null)
            {
                return false;
            }

            var defeatedEnemy = cell.Enemy;
            cell.ClearEnemy();
            EnemyDefeated?.Invoke(cell, defeatedEnemy);
            Grid.QueueRefresh();
            return true;
        }

        public void DamageEnemiesAround(Vector2I center, int radius, int damage)
        {
            foreach (var cell in Grid.EnumerateVisibleCells())
            {
                if (!cell.HasEnemy)
                {
                    continue;
                }

                var manhattan = Mathf.Abs(center.X - cell.GridPosition.X) + Mathf.Abs(center.Y - cell.GridPosition.Y);
                if (manhattan > radius)
                {
                    continue;
                }

                ResolveEnemyEncounter(cell);
            }
        }

        private void ProcessEnemy(CellData cell, float delta)
        {
            var enemy = cell.Enemy;
            var distance = Mathf.Abs(Stats.GridPosition.X - cell.GridPosition.X) + Mathf.Abs(Stats.GridPosition.Y - cell.GridPosition.Y);
            enemy.IsActive = distance <= 4;
            if (!enemy.IsActive)
            {
                return;
            }

            if (enemy.IsDebtCollector)
            {
                ProcessCollector(cell, enemy, delta);
                return;
            }

            if (enemy.Type == EnemyType.Bomber)
            {
                ProcessBomber(cell, enemy, distance, delta);
                return;
            }

            if (enemy.AttackInterval <= 0f)
            {
                return;
            }

            enemy.AttackTimer += delta;
            if (enemy.AttackTimer < enemy.AttackInterval)
            {
                return;
            }

            enemy.AttackTimer = 0f;
            if (enemy.Type == EnemyType.Shooter)
            {
                var direction = GetCardinalDirection(cell.GridPosition, Stats.GridPosition);
                BulletManager.SpawnBullet(cell.GridPosition, direction, enemy.BulletDamage);
            }
            else if (enemy.Type == EnemyType.Spreader)
            {
                BulletManager.SpawnBullet(cell.GridPosition, Vector2I.Up, enemy.BulletDamage);
                BulletManager.SpawnBullet(cell.GridPosition, Vector2I.Down, enemy.BulletDamage);
                BulletManager.SpawnBullet(cell.GridPosition, Vector2I.Left, enemy.BulletDamage);
                BulletManager.SpawnBullet(cell.GridPosition, Vector2I.Right, enemy.BulletDamage);
            }
        }

        private void ProcessBomber(CellData cell, EnemyData enemy, int distance, float delta)
        {
            if (distance <= 1)
            {
                enemy.FuseStarted = true;
            }

            if (!enemy.FuseStarted)
            {
                return;
            }

            enemy.FuseTimer -= delta;
            if (enemy.FuseTimer > 0f)
            {
                return;
            }

            if (Mathf.Abs(Stats.GridPosition.X - cell.GridPosition.X) <= 2 && Mathf.Abs(Stats.GridPosition.Y - cell.GridPosition.Y) <= 2)
            {
                Stats.TakeDamage(enemy.BulletDamage, ignoreInvincibility: true);
            }

            DamageEnemiesAround(cell.GridPosition, 2, enemy.BulletDamage);
            cell.ClearEnemy();
            Grid.QueueRefresh();
        }

        private void ProcessCollector(CellData cell, EnemyData enemy, float delta)
        {
            enemy.MoveTimer += delta;
            if (enemy.MoveTimer < 0.6f)
            {
                return;
            }

            enemy.MoveTimer = 0f;
            var step = DebtCollectorEnemy.GetStep(cell.GridPosition, Stats.GridPosition);
            var nextPosition = cell.GridPosition + step;
            if (nextPosition == Stats.GridPosition)
            {
                var stolenGold = (long)Mathf.Ceil(Stats.Gold * 0.2f);
                Stats.SpendGold(stolenGold);
                Stats.TakeDamage(DebtCollectorEnemy.ContactDamage, ignoreInvincibility: true);
                return;
            }

            var targetCell = Grid.GetCell(nextPosition.X, nextPosition.Y);
            if (targetCell == null || targetCell.Type == CellType.Boss || targetCell.HasEnemy || !targetCell.IsDiggable)
            {
                return;
            }

            if (targetCell.Type != CellType.Empty)
            {
                targetCell.Dig();
            }

            targetCell.SetEnemy(enemy, 1.2f);
            cell.ClearEnemy();
            Grid.QueueRefresh();
        }

        private static Vector2I GetCardinalDirection(Vector2I from, Vector2I to)
        {
            var delta = to - from;
            if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
            {
                return new Vector2I(delta.X > 0 ? 1 : -1, 0);
            }

            if (delta.Y != 0)
            {
                return new Vector2I(0, delta.Y > 0 ? 1 : -1);
            }

            return Vector2I.Down;
        }
    }
}