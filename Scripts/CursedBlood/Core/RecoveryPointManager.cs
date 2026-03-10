using System.Collections.Generic;
using Godot;

namespace CursedBlood.Core
{
    public readonly record struct RecoveryReturnState(bool IsAvailable, bool JustActivated, Vector2I Point, float SecondsRemaining, float DistanceCells)
    {
        public static readonly RecoveryReturnState Unavailable = new(false, false, default, 0f, float.MaxValue);
    }

    public partial class RecoveryPointManager : Node
    {
        private readonly List<Vector2I> _points = new();

        private Vector2I? _activePoint;
        private int _nextSpawnDepth;
        private float _retentionTimer;
        private bool _wasAvailable;

        public int ActivationRadiusCells { get; set; } = 8;

        public float RetentionTimeSeconds { get; set; } = 1.6f;

        public int RetentionDistanceCells { get; set; } = 10;

        public float SpawnReductionByDepth { get; set; } = 0.10f;

        public float SpawnReductionByChain { get; set; } = 1.4f;

        public int MinActivationRadiusCells { get; set; } = 4;

        public float ActivationRadiusPenaltyPer100m { get; set; } = 1f;

        public float ActivationRadiusPenaltyPerChain { get; set; } = 0.08f;

        public int MinRetentionDistanceCells { get; set; } = 6;

        public float RetentionDistancePenaltyPer100m { get; set; } = 0.9f;

        public float ProximitySlowdownMultiplier { get; set; } = 1.22f;

        public IReadOnlyList<Vector2I> Points => _points;

        public void Reset(ChunkManager chunks, Vector2I entrance)
        {
            _points.Clear();
            _activePoint = null;
            _retentionTimer = 0f;
            _wasAvailable = false;
            _nextSpawnDepth = entrance.Y + 24;
            TrySpawnPoint(chunks, entrance, _nextSpawnDepth);
            _nextSpawnDepth += GetSpawnSpacing(_nextSpawnDepth, 0);
        }

        public void UpdateSpawn(ChunkManager chunks, Vector2I anchor, int maxDepthRow, int currentChainCount)
        {
            if (chunks == null || maxDepthRow + 12 < _nextSpawnDepth)
            {
                return;
            }

            if (TrySpawnPoint(chunks, anchor, _nextSpawnDepth))
            {
                _nextSpawnDepth += GetSpawnSpacing(_nextSpawnDepth, currentChainCount);
            }
            else
            {
                _nextSpawnDepth += 14 + Mathf.RoundToInt(Mathf.Max(0, currentChainCount) * 0.6f);
            }
        }

        public RecoveryReturnState UpdateAvailability(Vector2I playerCenter, int playerSize, float delta, int currentDepthMeters, int currentChainCount)
        {
            _retentionTimer = Mathf.Max(0f, _retentionTimer - Mathf.Max(0f, delta));
            var activationRadius = ResolveActivationRadius(currentDepthMeters, currentChainCount);
            var retentionDistance = ResolveRetentionDistance(currentDepthMeters);

            var hasImmediatePoint = TryFindNearestPoint(playerCenter, activationRadius, out var immediatePoint, out var immediateDistance);
            if (hasImmediatePoint)
            {
                _activePoint = immediatePoint;
                _retentionTimer = RetentionTimeSeconds;
            }

            if (_activePoint.HasValue)
            {
                var distance = GetDistance(playerCenter, _activePoint.Value);
                if (!hasImmediatePoint && (_retentionTimer <= 0f || distance > retentionDistance))
                {
                    _activePoint = null;
                }
            }

            var isAvailable = _activePoint.HasValue;
            var justActivated = isAvailable && !_wasAvailable;
            _wasAvailable = isAvailable;

            if (!isAvailable)
            {
                return RecoveryReturnState.Unavailable;
            }

            var activeDistance = GetDistance(playerCenter, _activePoint.Value);
            return new RecoveryReturnState(true, justActivated, _activePoint.Value, _retentionTimer, activeDistance);
        }

        public bool IsReturnPointAvailable(Vector2I playerCenter, int playerSize)
        {
            return TryFindNearestPoint(playerCenter, ActivationRadiusCells, out _, out _);
        }

        private bool TrySpawnPoint(ChunkManager chunks, Vector2I anchor, int targetRow)
        {
            var bestScore = int.MaxValue;
            var bestPoint = new Vector2I(int.MinValue, int.MinValue);
            var startRow = Mathf.Max(Player.PlayerStats.StartGridPosition.Y + 10, targetRow - 10);
            var endRow = targetRow + 10;

            for (var row = startRow; row <= endRow; row++)
            {
                for (var dx = -36; dx <= 36; dx++)
                {
                    var col = anchor.X + dx;
                    if (!CanPlacePoint(chunks, col, row))
                    {
                        continue;
                    }

                    var score = Mathf.Abs(row - targetRow) * 6 + Mathf.Abs(dx) * 2;
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestPoint = new Vector2I(col, row);
                }
            }

            if (bestPoint.X == int.MinValue)
            {
                return false;
            }

            CarveLandingZone(chunks, bestPoint);
            chunks.SetCell(bestPoint.X, bestPoint.Y, (byte)CellType.RecoveryPoint);
            _points.Add(bestPoint);
            chunks.RequestRefresh();
            return true;
        }

        private bool TryFindNearestPoint(Vector2I playerCenter, int radiusCells, out Vector2I point, out float distance)
        {
            point = default;
            distance = float.MaxValue;
            var found = false;

            for (var index = 0; index < _points.Count; index++)
            {
                var candidate = _points[index];
                var candidateDistance = GetDistance(playerCenter, candidate);
                if (candidateDistance > radiusCells || candidateDistance >= distance)
                {
                    continue;
                }

                point = candidate;
                distance = candidateDistance;
                found = true;
            }

            return found;
        }

        private static float GetDistance(Vector2I from, Vector2I to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        private int GetSpawnSpacing(int row, int currentChainCount)
        {
            var depth = Mathf.Max(0, row - Player.PlayerStats.SurfaceRow);
            var baseSpacing = depth switch
            {
                < 160 => 42,
                < 420 => 58,
                < 900 => 74,
                _ => 92
            };

            var depthIncrease = baseSpacing * Mathf.Max(0f, SpawnReductionByDepth) * (depth / 100f);
            var chainIncrease = Mathf.Max(0, currentChainCount) * Mathf.Max(0f, SpawnReductionByChain);
            return Mathf.RoundToInt(baseSpacing + depthIncrease + chainIncrease);
        }

        private int ResolveActivationRadius(int currentDepthMeters, int currentChainCount)
        {
            var penalty = (currentDepthMeters / 100f) * Mathf.Max(0f, ActivationRadiusPenaltyPer100m);
            penalty += Mathf.Max(0, currentChainCount) * Mathf.Max(0f, ActivationRadiusPenaltyPerChain);
            return Mathf.Max(MinActivationRadiusCells, Mathf.RoundToInt(ActivationRadiusCells - penalty));
        }

        private int ResolveRetentionDistance(int currentDepthMeters)
        {
            var penalty = (currentDepthMeters / 100f) * Mathf.Max(0f, RetentionDistancePenaltyPer100m);
            return Mathf.Max(MinRetentionDistanceCells, Mathf.RoundToInt(RetentionDistanceCells - penalty));
        }

        private bool CanPlacePoint(ChunkManager chunks, int centerCol, int centerRow)
        {
            foreach (var point in _points)
            {
                if (Mathf.Abs(point.X - centerCol) <= 10 && Mathf.Abs(point.Y - centerRow) <= 10)
                {
                    return false;
                }
            }

            for (var row = centerRow - 3; row <= centerRow + 3; row++)
            {
                for (var col = centerCol - 3; col <= centerCol + 3; col++)
                {
                    var type = (CellType)chunks.GetCell(col, row);
                    if (type == CellType.Bedrock || type == CellType.RecoveryPoint)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void CarveLandingZone(ChunkManager chunks, Vector2I center)
        {
            for (var row = center.Y - 3; row <= center.Y + 3; row++)
            {
                for (var col = center.X - 3; col <= center.X + 3; col++)
                {
                    var type = (CellType)chunks.GetCell(col, row);
                    if (type == CellType.Bedrock)
                    {
                        continue;
                    }

                    chunks.SetCell(col, row, (byte)CellType.Empty);
                }
            }
        }
    }
}