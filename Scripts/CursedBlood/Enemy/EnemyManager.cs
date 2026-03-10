using System;
using System.Collections.Generic;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Enemy
{
    public sealed class EnemyManager
    {
        private readonly List<EnemyState> _enemies = new();
        private readonly RandomNumberGenerator _rng = new();

        private float _spawnTimer;
        private int _nextEnemyId = 1;
        private string _pendingNotification = string.Empty;

        public float SpawnCheckInterval { get; set; } = 0.85f;

        public int MaxActiveEnemies { get; set; } = 10;

        public float BaseSpawnChance { get; set; } = 0.05f;

        public float SpawnChancePerDepthMeter { get; set; } = 0.0018f;

        public float SpawnChancePerChain { get; set; } = 0.028f;

        public float MaxSpawnChance { get; set; } = 0.84f;

        public float MoveIntervalSeconds { get; set; } = 0.72f;

        public float ContactCooldownSeconds { get; set; } = 1.15f;

        public float DamageMultiplier { get; set; } = 1f;

        public float OxygenPenaltyMultiplier { get; set; } = 1f;

        public float MoveSlowdownMultiplier { get; set; } = 1f;

        public float DigSlowdownMultiplier { get; set; } = 1f;

        public float DebuffDurationMultiplier { get; set; } = 1f;

        public int SpawnDistanceMin { get; set; } = 10;

        public int SpawnDistanceMax { get; set; } = 20;

        public int DespawnDistanceCells { get; set; } = 34;

        public int ContactPaddingCells { get; set; } = 1;

        public int DesiredEnemyDepthStep { get; set; } = 90;

        public int DesiredEnemyChainStep { get; set; } = 6;

        public float SonarDangerWeight { get; set; } = 0.78f;

        public EnemyDangerReading CurrentDanger { get; private set; } = EnemyDangerReading.None;

        public int ActiveCount => _enemies.Count;

        public void Reset(ChunkManager chunks, Vector2I entrance)
        {
            if (chunks != null)
            {
                for (var index = 0; index < _enemies.Count; index++)
                {
                    var enemy = _enemies[index];
                    chunks.SetTransientCell(enemy.Cell.X, enemy.Cell.Y, (byte)CellType.Empty);
                }
            }

            _rng.Randomize();
            _enemies.Clear();
            _spawnTimer = SpawnCheckInterval;
            _nextEnemyId = 1;
            _pendingNotification = string.Empty;
            CurrentDanger = EnemyDangerReading.None;
        }

        public void Update(float delta, ChunkManager chunks, Vector2I playerCell, int playerSize, PlayerStats stats)
        {
            if (chunks == null || stats == null)
            {
                return;
            }

            var clampedDelta = Mathf.Max(0f, delta);
            for (var index = 0; index < _enemies.Count; index++)
            {
                var enemy = _enemies[index];
                enemy.MoveCooldown = Mathf.Max(0f, enemy.MoveCooldown - clampedDelta);
                enemy.ContactCooldown = Mathf.Max(0f, enemy.ContactCooldown - clampedDelta);
            }

            CullInvalidEnemies(chunks, playerCell);

            _spawnTimer -= clampedDelta;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = SpawnCheckInterval;
                TrySpawnEnemies(chunks, playerCell, playerSize, stats);
            }

            UpdateEnemyMovement(chunks, playerCell, playerSize);
            ResolveThreatContacts(chunks, playerCell, playerSize, stats);
            CurrentDanger = BuildDangerReading(playerCell, stats.CurrentDepthMeters, stats.CurrentChainCount);
        }

        public bool TryResolveDrillContact(ChunkManager chunks, IReadOnlyList<Vector2I> digArea, Vector2I moveDirection, PlayerStats stats)
        {
            if (chunks == null || stats == null || digArea == null || digArea.Count == 0)
            {
                return false;
            }

            EnemyState target = null;
            var bestDistance = int.MaxValue;
            var origin = stats.GridPosition;

            for (var index = 0; index < _enemies.Count; index++)
            {
                var enemy = _enemies[index];
                if (!ContainsCell(digArea, enemy.Cell))
                {
                    continue;
                }

                var delta = enemy.Cell - origin;
                if ((delta.X * moveDirection.X) + (delta.Y * moveDirection.Y) <= 0)
                {
                    continue;
                }

                var distance = Mathf.Abs(delta.X) + Mathf.Abs(delta.Y);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                target = enemy;
            }

            if (target == null)
            {
                return false;
            }

            RemoveEnemy(chunks, target);
            ApplyPenalty(stats, EnemyData.Create(target.Type), defeatedByDrill: true);
            return true;
        }

        public bool TryConsumeNotification(out string message)
        {
            if (string.IsNullOrWhiteSpace(_pendingNotification))
            {
                message = string.Empty;
                return false;
            }

            message = _pendingNotification;
            _pendingNotification = string.Empty;
            return true;
        }

        private void CullInvalidEnemies(ChunkManager chunks, Vector2I playerCell)
        {
            for (var index = _enemies.Count - 1; index >= 0; index--)
            {
                var enemy = _enemies[index];
                var dx = Mathf.Abs(enemy.Cell.X - playerCell.X);
                var dy = Mathf.Abs(enemy.Cell.Y - playerCell.Y);
                if (dx > DespawnDistanceCells || dy > DespawnDistanceCells)
                {
                    RemoveEnemy(chunks, enemy);
                    continue;
                }

                if ((CellType)chunks.GetCell(enemy.Cell.X, enemy.Cell.Y) != CellType.Enemy)
                {
                    _enemies.RemoveAt(index);
                }
            }
        }

        private void TrySpawnEnemies(ChunkManager chunks, Vector2I playerCell, int playerSize, PlayerStats stats)
        {
            var desiredCount = ResolveDesiredEnemyCount(stats.CurrentDepthMeters, stats.CurrentChainCount);
            if (_enemies.Count >= desiredCount || _enemies.Count >= MaxActiveEnemies)
            {
                return;
            }

            var spawnChance = Mathf.Clamp(
                BaseSpawnChance + (stats.CurrentDepthMeters * SpawnChancePerDepthMeter) + (stats.CurrentChainCount * SpawnChancePerChain),
                0f,
                MaxSpawnChance);

            if (_rng.Randf() > spawnChance)
            {
                return;
            }

            var spawnBudget = Math.Min(2, desiredCount - _enemies.Count);
            for (var spawnIndex = 0; spawnIndex < spawnBudget; spawnIndex++)
            {
                if (!TrySpawnSingle(chunks, playerCell, playerSize))
                {
                    break;
                }
            }
        }

        private bool TrySpawnSingle(ChunkManager chunks, Vector2I playerCell, int playerSize)
        {
            for (var attempt = 0; attempt < 42; attempt++)
            {
                var offset = new Vector2I(
                    _rng.RandiRange(-SpawnDistanceMax, SpawnDistanceMax),
                    _rng.RandiRange(-4, SpawnDistanceMax));
                var distance = Mathf.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
                if (distance < SpawnDistanceMin || distance > SpawnDistanceMax)
                {
                    continue;
                }

                var candidate = playerCell + offset;
                if (!chunks.IsInBounds(candidate.X, candidate.Y))
                {
                    continue;
                }

                if (IsInsidePlayerBody(candidate, playerCell, playerSize, padding: 4))
                {
                    continue;
                }

                if (HasEnemyAt(candidate))
                {
                    continue;
                }

                if ((CellType)chunks.GetCell(candidate.X, candidate.Y) != CellType.Empty)
                {
                    continue;
                }

                var type = _rng.Randf() < 0.5f ? EnemyType.ThornMite : EnemyType.GasLeech;
                var enemy = new EnemyState(_nextEnemyId++, type, candidate, MoveIntervalSeconds * _rng.RandfRange(0.75f, 1.2f), ContactCooldownSeconds * 0.5f);
                _enemies.Add(enemy);
                chunks.SetTransientCell(candidate.X, candidate.Y, (byte)CellType.Enemy);
                return true;
            }

            return false;
        }

        private void UpdateEnemyMovement(ChunkManager chunks, Vector2I playerCell, int playerSize)
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                var enemy = _enemies[index];
                if (enemy.MoveCooldown > 0f)
                {
                    continue;
                }

                enemy.MoveCooldown = MoveIntervalSeconds * _rng.RandfRange(0.82f, 1.18f);
                var nextCell = ResolveNextStep(chunks, enemy.Cell, playerCell, playerSize);
                if (nextCell == enemy.Cell)
                {
                    continue;
                }

                chunks.SetTransientCell(enemy.Cell.X, enemy.Cell.Y, (byte)CellType.Empty);
                enemy.Cell = nextCell;
                chunks.SetTransientCell(enemy.Cell.X, enemy.Cell.Y, (byte)CellType.Enemy);
            }
        }

        private void ResolveThreatContacts(ChunkManager chunks, Vector2I playerCell, int playerSize, PlayerStats stats)
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                var enemy = _enemies[index];
                if (enemy.ContactCooldown > 0f || !IsThreateningPlayer(enemy.Cell, playerCell, playerSize))
                {
                    continue;
                }

                enemy.ContactCooldown = ContactCooldownSeconds;
                ApplyPenalty(stats, EnemyData.Create(enemy.Type), defeatedByDrill: false);
                TryRetreatEnemy(chunks, enemy, playerCell, playerSize);
                break;
            }
        }

        private void TryRetreatEnemy(ChunkManager chunks, EnemyState enemy, Vector2I playerCell, int playerSize)
        {
            var awayDirection = new Vector2I(Mathf.Sign(enemy.Cell.X - playerCell.X), Mathf.Sign(enemy.Cell.Y - playerCell.Y));
            var candidates = new[]
            {
                awayDirection,
                new Vector2I(awayDirection.X, 0),
                new Vector2I(0, awayDirection.Y),
                new Vector2I(-awayDirection.Y, awayDirection.X),
                new Vector2I(awayDirection.Y, -awayDirection.X)
            };

            for (var index = 0; index < candidates.Length; index++)
            {
                var direction = candidates[index];
                if (direction == Vector2I.Zero)
                {
                    continue;
                }

                var candidate = enemy.Cell + direction;
                if (!IsWalkableEnemyCell(chunks, candidate, playerCell, playerSize))
                {
                    continue;
                }

                chunks.SetTransientCell(enemy.Cell.X, enemy.Cell.Y, (byte)CellType.Empty);
                enemy.Cell = candidate;
                chunks.SetTransientCell(enemy.Cell.X, enemy.Cell.Y, (byte)CellType.Enemy);
                return;
            }
        }

        private Vector2I ResolveNextStep(ChunkManager chunks, Vector2I origin, Vector2I playerCell, int playerSize)
        {
            var dx = Mathf.Sign(playerCell.X - origin.X);
            var dy = Mathf.Sign(playerCell.Y - origin.Y);
            var preferHorizontal = Mathf.Abs(playerCell.X - origin.X) > Mathf.Abs(playerCell.Y - origin.Y);

            var candidates = preferHorizontal
                ? new[]
                {
                    new Vector2I(dx, 0),
                    new Vector2I(dx, dy),
                    new Vector2I(0, dy),
                    new Vector2I(0, -dy),
                    new Vector2I(-dx, 0)
                }
                : new[]
                {
                    new Vector2I(0, dy),
                    new Vector2I(dx, dy),
                    new Vector2I(dx, 0),
                    new Vector2I(-dx, 0),
                    new Vector2I(0, -dy)
                };

            for (var index = 0; index < candidates.Length; index++)
            {
                var direction = candidates[index];
                if (direction == Vector2I.Zero)
                {
                    continue;
                }

                var candidate = origin + direction;
                if (IsWalkableEnemyCell(chunks, candidate, playerCell, playerSize))
                {
                    return candidate;
                }
            }

            return origin;
        }

        private EnemyDangerReading BuildDangerReading(Vector2I playerCell, int currentDepthMeters, int currentChainCount)
        {
            if (_enemies.Count == 0)
            {
                return EnemyDangerReading.None;
            }

            var nearestCell = Vector2I.Zero;
            var nearestDistance = float.MaxValue;
            var nearbyCount = 0;
            for (var index = 0; index < _enemies.Count; index++)
            {
                var enemy = _enemies[index];
                var distance = enemy.Cell.DistanceTo(playerCell);
                if (distance <= 12f)
                {
                    nearbyCount++;
                }

                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestCell = enemy.Cell;
            }

            if (nearestDistance > 28f)
            {
                return EnemyDangerReading.None;
            }

            var depthPressure = Mathf.Clamp(currentDepthMeters / 220f, 0f, 1.4f);
            var chainPressure = Mathf.Clamp(currentChainCount / 12f, 0f, 1.2f);
            return new EnemyDangerReading(true, nearestCell, nearbyCount, depthPressure + chainPressure);
        }

        private void ApplyPenalty(PlayerStats stats, EnemyData data, bool defeatedByDrill)
        {
            stats.ApplyHazard(
                Mathf.RoundToInt(data.ContactDamage * Mathf.Max(0f, DamageMultiplier)),
                data.OxygenPenaltySeconds * Mathf.Max(0f, OxygenPenaltyMultiplier),
                1f + ((data.MoveSlowdownMultiplier - 1f) * Mathf.Max(0f, MoveSlowdownMultiplier)),
                1f + ((data.DigSlowdownMultiplier - 1f) * Mathf.Max(0f, DigSlowdownMultiplier)),
                data.DebuffDurationSeconds * Mathf.Max(0f, DebuffDurationMultiplier),
                data.StatusLabel);

            _pendingNotification = defeatedByDrill
                ? $"{data.DisplayName}を突破 / 被害を受けた"
                : $"{data.DisplayName}が接触 / {data.StatusLabel}";
        }

        private int ResolveDesiredEnemyCount(int currentDepthMeters, int currentChainCount)
        {
            if (currentDepthMeters < 24 && currentChainCount <= 0)
            {
                return 0;
            }

            var depthContribution = Mathf.FloorToInt(currentDepthMeters / Mathf.Max(24, DesiredEnemyDepthStep));
            var chainContribution = Mathf.FloorToInt(currentChainCount / Mathf.Max(2, DesiredEnemyChainStep));
            return Mathf.Clamp(1 + depthContribution + chainContribution, 0, MaxActiveEnemies);
        }

        private bool IsWalkableEnemyCell(ChunkManager chunks, Vector2I candidate, Vector2I playerCell, int playerSize)
        {
            if (!chunks.IsInBounds(candidate.X, candidate.Y) || HasEnemyAt(candidate) || IsInsidePlayerBody(candidate, playerCell, playerSize, 0))
            {
                return false;
            }

            return (CellType)chunks.GetCell(candidate.X, candidate.Y) == CellType.Empty;
        }

        private bool IsThreateningPlayer(Vector2I enemyCell, Vector2I playerCell, int playerSize)
        {
            return IsInsidePlayerBody(enemyCell, playerCell, playerSize, ContactPaddingCells);
        }

        private static bool IsInsidePlayerBody(Vector2I cell, Vector2I playerCell, int playerSize, int padding)
        {
            var half = playerSize / 2;
            return Mathf.Abs(cell.X - playerCell.X) <= half + padding && Mathf.Abs(cell.Y - playerCell.Y) <= half + padding;
        }

        private static bool ContainsCell(IReadOnlyList<Vector2I> cells, Vector2I target)
        {
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEnemyAt(Vector2I cell)
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                if (_enemies[index].Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveEnemy(ChunkManager chunks, EnemyState enemy)
        {
            chunks?.SetTransientCell(enemy.Cell.X, enemy.Cell.Y, (byte)CellType.Empty);
            for (var index = _enemies.Count - 1; index >= 0; index--)
            {
                if (_enemies[index].Id == enemy.Id)
                {
                    _enemies.RemoveAt(index);
                    break;
                }
            }
        }
    }
}