using System;
using System.Linq;
using CursedBlood.Config;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Enemy
{
    public partial class BossController : Node2D
    {
        private readonly Random _rng = new();

        public GridManager Grid { get; set; }

        public PlayerStats Stats { get; set; }

        public BulletManager BulletManager { get; set; }

        public BalanceConfig BalanceConfig { get; set; }

        public bool SimulationEnabled { get; set; } = true;

        public BossData ActiveBoss { get; private set; }

        public event Action<BossData> BossDefeated;

        public override void _Process(double delta)
        {
            if (!SimulationEnabled || Grid == null || Stats == null || BulletManager == null || BalanceConfig == null)
            {
                return;
            }

            EnsureActiveBoss();
            if (ActiveBoss == null || ActiveBoss.IsDefeated)
            {
                return;
            }

            if (ActiveBoss.WarningColumn >= 0)
            {
                ActiveBoss.WarningTimer -= (float)delta;
                if (ActiveBoss.WarningTimer <= 0f)
                {
                    if (Stats.GridPosition.X == ActiveBoss.WarningColumn)
                    {
                        Stats.TakeDamage(ActiveBoss.IsDemonLord ? 60 : 25, ignoreInvincibility: true);
                    }

                    ActiveBoss.WarningColumn = -1;
                }
            }

            ActiveBoss.AttackTimer += (float)delta;
            var interval = GetAttackInterval(ActiveBoss);
            if (ActiveBoss.AttackTimer < interval)
            {
                return;
            }

            ActiveBoss.AttackTimer = 0f;
            FireBossPattern(ActiveBoss);
        }

        public bool TryAttack(Vector2I playerPosition, Vector2I direction)
        {
            EnsureActiveBoss();
            if (ActiveBoss == null || ActiveBoss.IsDefeated)
            {
                return false;
            }

            var targetPosition = playerPosition + direction;
            if (!BossArena.EnumerateBossCells(ActiveBoss.CenterPosition, ActiveBoss.IsDemonLord).Contains(targetPosition))
            {
                return false;
            }

            var damage = Stats.CalculateAttackDamage(againstBoss: true);
            ActiveBoss.TakeDamage(Mathf.Max(1, damage));
            Stats.AddSkillGauge(BalanceConfig.SkillChargePerBossHit);
            if (!ActiveBoss.IsDefeated)
            {
                return true;
            }

            foreach (var bossCell in BossArena.EnumerateBossCells(ActiveBoss.CenterPosition, ActiveBoss.IsDemonLord))
            {
                Grid.GetCell(bossCell.X, bossCell.Y)?.ClearBoss();
            }

            BossDefeated?.Invoke(ActiveBoss);
            Grid.QueueRefresh();
            return true;
        }

        public void DealDirectDamage(int damage)
        {
            EnsureActiveBoss();
            if (ActiveBoss == null || ActiveBoss.IsDefeated)
            {
                return;
            }

            ActiveBoss.TakeDamage(Mathf.Max(1, damage));
            if (!ActiveBoss.IsDefeated)
            {
                return;
            }

            foreach (var bossCell in BossArena.EnumerateBossCells(ActiveBoss.CenterPosition, ActiveBoss.IsDemonLord))
            {
                Grid.GetCell(bossCell.X, bossCell.Y)?.ClearBoss();
            }

            BossDefeated?.Invoke(ActiveBoss);
            Grid.QueueRefresh();
        }

        public void Reset()
        {
            ActiveBoss = null;
        }

        private void EnsureActiveBoss()
        {
            if (ActiveBoss != null && !ActiveBoss.IsDefeated)
            {
                return;
            }

            var bossCell = Grid.EnumerateVisibleCells().FirstOrDefault(cell => cell.HasBoss);
            if (bossCell?.BossCell == null)
            {
                ActiveBoss = null;
                return;
            }

            ActiveBoss = CreateBossData(bossCell.BossCell);
        }

        private BossData CreateBossData(BossCellData bossCell)
        {
            var maxHp = bossCell.IsDemonLord
                ? Mathf.RoundToInt(BalanceConfig.DemonLordHp)
                : Mathf.RoundToInt(BalanceConfig.BossBaseHp * Mathf.Pow(BalanceConfig.BossHpGrowth, bossCell.Depth / 100f - 1f));

            return new BossData
            {
                Depth = bossCell.Depth,
                CenterPosition = bossCell.CenterPosition,
                MaxHp = maxHp,
                CurrentHp = maxHp,
                DepthTier = GridGenerator.GetDepthTier(bossCell.Depth),
                IsDemonLord = bossCell.IsDemonLord
            };
        }

        private static float GetAttackInterval(BossData boss)
        {
            return boss.IsDemonLord
                ? boss.CurrentPhase switch
                {
                    BossPhase.Phase1 => 3f,
                    BossPhase.Phase2 => 2.2f,
                    BossPhase.Phase3 => 1.8f,
                    _ => 1.2f
                }
                : boss.CurrentPhase switch
                {
                    BossPhase.Phase1 => 3f,
                    BossPhase.Phase2 => 2f,
                    _ => 1.5f
                };
        }

        private void FireBossPattern(BossData boss)
        {
            var center = boss.CenterPosition;
            switch (boss.CurrentPhase)
            {
                case BossPhase.Phase1:
                    FireCardinalBurst(center, boss.IsDemonLord ? 16 : 8, boss.IsDemonLord ? 18 : 12, boss.IsDemonLord);
                    if (boss.IsDemonLord)
                    {
                        FireHoming(center, 2, 20);
                    }

                    break;
                case BossPhase.Phase2:
                    FireEightDirection(center, boss.IsDemonLord ? 16 : 12, boss.IsDemonLord);
                    break;
                case BossPhase.Phase3:
                    FireOmni(center, boss.IsDemonLord ? 24 : 16, boss.IsDemonLord);
                    BeginColumnWarning(boss);
                    if (boss.IsDemonLord)
                    {
                        FireHoming(center, 1, 24);
                    }

                    break;
                case BossPhase.Phase4:
                    FireOmni(center, 28, true);
                    BeginColumnWarning(boss);
                    FireHoming(center, 2, 28);
                    break;
            }
        }

        private void FireCardinalBurst(Vector2I center, int damage, int spacing, bool isBossBullet)
        {
            BulletManager.SpawnBullet(center, Vector2I.Up, damage, isBossBullet);
            BulletManager.SpawnBullet(center, Vector2I.Down, damage, isBossBullet);
            BulletManager.SpawnBullet(center, Vector2I.Left, damage, isBossBullet);
            BulletManager.SpawnBullet(center, Vector2I.Right, damage, isBossBullet);
            BulletManager.SpawnBullet(center + new Vector2I(0, 1), Vector2I.Up, damage, isBossBullet);
            BulletManager.SpawnBullet(center + new Vector2I(0, -1), Vector2I.Down, damage, isBossBullet);
            BulletManager.SpawnBullet(center + new Vector2I(1, 0), Vector2I.Left, damage, isBossBullet);
            BulletManager.SpawnBullet(center + new Vector2I(-1, 0), Vector2I.Right, damage, isBossBullet);
        }

        private void FireEightDirection(Vector2I center, int damage, bool isBossBullet)
        {
            var directions = new[]
            {
                Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right,
                new Vector2I(-1, -1), new Vector2I(1, -1), new Vector2I(-1, 1), new Vector2I(1, 1)
            };

            foreach (var direction in directions)
            {
                BulletManager.SpawnBullet(center, direction, damage, isBossBullet);
            }
        }

        private void FireOmni(Vector2I center, int damage, bool isBossBullet)
        {
            FireEightDirection(center, damage, isBossBullet);
            BulletManager.SpawnBullet(center, new Vector2I(2, 1), damage, isBossBullet);
            BulletManager.SpawnBullet(center, new Vector2I(-2, 1), damage, isBossBullet);
            BulletManager.SpawnBullet(center, new Vector2I(2, -1), damage, isBossBullet);
            BulletManager.SpawnBullet(center, new Vector2I(-2, -1), damage, isBossBullet);
        }

        private void FireHoming(Vector2I center, int count, int damage)
        {
            for (var index = 0; index < count; index++)
            {
                BulletManager.SpawnBullet(center, Vector2I.Down, damage, true, true);
            }
        }

        private void BeginColumnWarning(BossData boss)
        {
            boss.WarningColumn = _rng.Next(0, GridManager.Columns);
            boss.WarningTimer = 1.5f;
        }
    }
}