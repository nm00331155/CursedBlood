using System;
using System.Collections.Generic;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Enemy
{
    public partial class BulletManager : Node2D
    {
        public sealed class BulletData
        {
            public Vector2I Position { get; set; }

            public Vector2I Direction { get; set; }

            public float MoveTimer { get; set; }

            public int Damage { get; set; }

            public int CellsTraveled { get; set; }

            public bool IsBossBullet { get; set; }

            public bool IsHoming { get; set; }
        }

        private readonly List<BulletData> _bullets = new();

        public GridManager Grid { get; set; }

        public PlayerStats Stats { get; set; }

        public float CellDuration { get; set; } = 0.5f;

        public bool SimulationEnabled { get; set; } = true;

        public System.Func<bool> IsPlayerGuarding { get; set; }

        public event Action BulletGuarded;

        public override void _Process(double delta)
        {
            if (!SimulationEnabled || Grid == null || Stats == null || _bullets.Count == 0)
            {
                return;
            }

            var shouldRedraw = false;
            for (var index = _bullets.Count - 1; index >= 0; index--)
            {
                var bullet = _bullets[index];
                bullet.MoveTimer += (float)delta;
                if (bullet.MoveTimer < CellDuration)
                {
                    continue;
                }

                bullet.MoveTimer -= CellDuration;
                if (bullet.IsHoming)
                {
                    bullet.Direction = GetHomingDirection(bullet.Position, Stats.GridPosition);
                }

                var nextPosition = bullet.Position + bullet.Direction;
                if (nextPosition == Stats.GridPosition)
                {
                    if (IsPlayerGuarding?.Invoke() == true)
                    {
                        BulletGuarded?.Invoke();
                    }
                    else
                    {
                        Stats.TakeDamage(bullet.Damage);
                    }

                    _bullets.RemoveAt(index);
                    shouldRedraw = true;
                    continue;
                }

                var nextCell = Grid.GetCell(nextPosition.X, nextPosition.Y);
                if (nextCell == null)
                {
                    _bullets.RemoveAt(index);
                    shouldRedraw = true;
                    continue;
                }

                if (nextCell.Type != CellType.Empty)
                {
                    _bullets.RemoveAt(index);
                    shouldRedraw = true;
                    continue;
                }

                bullet.Position = nextPosition;
                bullet.CellsTraveled++;
                shouldRedraw = true;
                if (bullet.CellsTraveled >= (bullet.IsHoming ? 3 : 2))
                {
                    _bullets.RemoveAt(index);
                }
            }

            if (shouldRedraw)
            {
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if (Grid == null)
            {
                return;
            }

            foreach (var bullet in _bullets)
            {
                var world = Grid.GridToWorld(bullet.Position.X, bullet.Position.Y);
                var color = bullet.IsBossBullet ? new Color(1f, 0.55f, 0.25f) : new Color(0.95f, 0.18f, 0.18f);
                DrawCircle(world, bullet.IsBossBullet ? 24f : 20f, color);
                DrawArc(world, bullet.IsBossBullet ? 28f : 24f, 0f, Mathf.Tau, 24, Colors.White, 2f);
            }
        }

        public void SpawnBullet(Vector2I origin, Vector2I direction, int damage, bool isBossBullet = false, bool isHoming = false)
        {
            if (direction == Vector2I.Zero)
            {
                return;
            }

            _bullets.Add(new BulletData
            {
                Position = origin,
                Direction = direction,
                Damage = damage,
                IsBossBullet = isBossBullet,
                IsHoming = isHoming
            });
            QueueRedraw();
        }

        public void ClearAll()
        {
            _bullets.Clear();
            QueueRedraw();
        }

        private static Vector2I GetHomingDirection(Vector2I from, Vector2I to)
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

            return Vector2I.Zero;
        }
    }
}