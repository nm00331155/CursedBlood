using System.Collections.Generic;
using Godot;

namespace CursedBlood.Core
{
    public partial class RecoveryPointManager : Node
    {
        private readonly List<Vector2I> _points = new();
        private int _nextSpawnDepth;

        public IReadOnlyList<Vector2I> Points => _points;

        public void Reset(ChunkManager chunks, int entranceRow)
        {
            _points.Clear();
            _nextSpawnDepth = entranceRow + 22;
            TrySpawnPoint(chunks, _nextSpawnDepth);
            _nextSpawnDepth += GetSpawnSpacing(_nextSpawnDepth);
        }

        public void UpdateSpawn(ChunkManager chunks, int maxDepthRow)
        {
            if (chunks == null || maxDepthRow + 12 < _nextSpawnDepth)
            {
                return;
            }

            if (TrySpawnPoint(chunks, _nextSpawnDepth))
            {
                _nextSpawnDepth += GetSpawnSpacing(_nextSpawnDepth);
            }
            else
            {
                _nextSpawnDepth += 12;
            }
        }

        public bool IsReturnPointAvailable(Vector2I playerCenter, int playerSize)
        {
            return FindPoint(playerCenter, playerSize, out _);
        }

        public bool TryConsumeAt(Vector2I playerCenter, int playerSize, ChunkManager chunks, out Vector2I point)
        {
            if (!FindPoint(playerCenter, playerSize, out point))
            {
                return false;
            }

            _points.Remove(point);
            chunks.SetCell(point.X, point.Y, (byte)CellType.Empty);
            chunks.RequestRefresh();
            return true;
        }

        private bool TrySpawnPoint(ChunkManager chunks, int targetRow)
        {
            var bestScore = int.MaxValue;
            var bestPoint = new Vector2I(-1, -1);

            for (var row = Mathf.Max(Player.PlayerStats.StartGridPosition.Y + 8, targetRow - 8); row <= targetRow + 8; row++)
            {
                for (var col = 6; col < ChunkManager.Columns - 6; col++)
                {
                    if (!CanPlacePoint(chunks, col, row))
                    {
                        continue;
                    }

                    var score = Mathf.Abs(row - targetRow) * 6 + Mathf.Abs(col - (ChunkManager.Columns / 2));
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestPoint = new Vector2I(col, row);
                }
            }

            if (bestPoint.X < 0)
            {
                return false;
            }

            CarveLandingZone(chunks, bestPoint);
            chunks.SetCell(bestPoint.X, bestPoint.Y, (byte)CellType.RecoveryPoint);
            _points.Add(bestPoint);
            chunks.RequestRefresh();
            return true;
        }

        private bool FindPoint(Vector2I playerCenter, int playerSize, out Vector2I point)
        {
            var half = playerSize / 2;
            for (var index = 0; index < _points.Count; index++)
            {
                var candidate = _points[index];
                if (Mathf.Abs(candidate.X - playerCenter.X) <= half && Mathf.Abs(candidate.Y - playerCenter.Y) <= half)
                {
                    point = candidate;
                    return true;
                }
            }

            point = default;
            return false;
        }

        private static int GetSpawnSpacing(int row)
        {
            return row switch
            {
                < 200 => 44,
                < 500 => 60,
                < 1000 => 76,
                < 3000 => 96,
                _ => 120
            };
        }

        private bool CanPlacePoint(ChunkManager chunks, int centerCol, int centerRow)
        {
            foreach (var point in _points)
            {
                if (Mathf.Abs(point.X - centerCol) <= 8 && Mathf.Abs(point.Y - centerRow) <= 8)
                {
                    return false;
                }
            }

            for (var row = centerRow - 2; row <= centerRow + 2; row++)
            {
                for (var col = centerCol - 2; col <= centerCol + 2; col++)
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
            for (var row = center.Y - 2; row <= center.Y + 2; row++)
            {
                for (var col = center.X - 2; col <= center.X + 2; col++)
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