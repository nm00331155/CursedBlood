using Godot;

namespace CursedBlood.Core
{
    public enum SonarTargetKind
    {
        None,
        ChainCheckpoint,
        RecoveryPoint,
        Ore,
        Item,
        Danger
    }

    public enum SonarSignalStrength
    {
        None,
        Far,
        Medium,
        Near
    }

    public readonly record struct SonarReading(SonarSignalStrength Strength, SonarTargetKind TargetKind, CellType TargetType, Vector2I Direction, int DistanceCells, Vector2I TargetCell)
    {
        public string GetDisplayText()
        {
            if (Strength == SonarSignalStrength.None)
            {
                return "ソナー: 反応なし";
            }

            var directionLabel = Direction switch
            {
                { X: 0, Y: -1 } => "上",
                { X: 1, Y: -1 } => "右上",
                { X: 1, Y: 0 } => "右",
                { X: 1, Y: 1 } => "右下",
                { X: 0, Y: 1 } => "下",
                { X: -1, Y: 1 } => "左下",
                { X: -1, Y: 0 } => "左",
                { X: -1, Y: -1 } => "左上",
                _ => "不明"
            };

            return Strength switch
            {
                SonarSignalStrength.Far => $"ソナー: 微反応 / {directionLabel} / {DescribeTarget(TargetKind, TargetType)} / {DistanceCells}m先",
                SonarSignalStrength.Medium => $"ソナー: {directionLabel} / {DescribeTarget(TargetKind, TargetType)} / {DistanceCells}m",
                SonarSignalStrength.Near => $"ソナー: {directionLabel} / {DescribeTarget(TargetKind, TargetType)} / {DistanceCells}m",
                _ => "ソナー: 反応なし"
            };
        }

        private static string DescribeTarget(SonarTargetKind kind, CellType type)
        {
            if (kind == SonarTargetKind.ChainCheckpoint)
            {
                return "チェイン目標";
            }

            return type switch
            {
                CellType.RecoveryPoint => "回収ポイント",
                CellType.Ore => "鉱脈",
                CellType.Item => "重要物",
                CellType.Enemy => "危険反応",
                _ => "反応源"
            };
        }
    }

    public sealed class SonarSystem
    {
        private const float ScanInterval = 0.12f;
        private const int BaseRadius = 28;

        private float _scanTimer;

        public SonarReading CurrentReading { get; private set; } = new(SonarSignalStrength.None, SonarTargetKind.None, CellType.Empty, Vector2I.Zero, 0, Vector2I.Zero);

        public void Reset()
        {
            _scanTimer = 0f;
            CurrentReading = new SonarReading(SonarSignalStrength.None, SonarTargetKind.None, CellType.Empty, Vector2I.Zero, 0, Vector2I.Zero);
        }

        public void Update(float delta, ChunkManager chunks, Vector2I origin, Vector2I? chainCheckpoint, int bonusRadius, float guidanceStrength, Vector2I? dangerCell, float dangerWeight)
        {
            _scanTimer -= delta;
            if (_scanTimer > 0f)
            {
                return;
            }

            _scanTimer = ScanInterval;
            CurrentReading = Scan(chunks, origin, chainCheckpoint, BaseRadius + Mathf.Max(0, bonusRadius), guidanceStrength, dangerCell, dangerWeight);
        }

        private static SonarReading Scan(ChunkManager chunks, Vector2I origin, Vector2I? chainCheckpoint, int maxRadius, float guidanceStrength, Vector2I? dangerCell, float dangerWeight)
        {
            var bestScore = float.MaxValue;
            var bestDistanceSquared = int.MaxValue;
            var bestCell = Vector2I.Zero;
            var bestType = CellType.Empty;
            var bestKind = SonarTargetKind.None;

            for (var row = origin.Y - maxRadius; row <= origin.Y + maxRadius; row++)
            {
                for (var col = origin.X - maxRadius; col <= origin.X + maxRadius; col++)
                {
                    if (!chunks.IsInBounds(col, row))
                    {
                        continue;
                    }

                    var type = (CellType)chunks.GetCell(col, row);
                    var kind = type switch
                    {
                        CellType.RecoveryPoint => SonarTargetKind.RecoveryPoint,
                        CellType.Ore => SonarTargetKind.Ore,
                        CellType.Item => SonarTargetKind.Item,
                        _ => SonarTargetKind.None
                    };

                    if (kind == SonarTargetKind.None)
                    {
                        continue;
                    }

                    var dx = col - origin.X;
                    var dy = row - origin.Y;
                    var distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared == 0 || distanceSquared > maxRadius * maxRadius)
                    {
                        continue;
                    }

                    var score = distanceSquared;
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestDistanceSquared = distanceSquared;
                    bestCell = new Vector2I(col, row);
                    bestType = type;
                    bestKind = kind;
                }
            }

            if (chainCheckpoint.HasValue && chunks.IsInBounds(chainCheckpoint.Value.X, chainCheckpoint.Value.Y))
            {
                var delta = chainCheckpoint.Value - origin;
                var distanceSquared = (delta.X * delta.X) + (delta.Y * delta.Y);
                if (distanceSquared > 0 && distanceSquared <= maxRadius * maxRadius)
                {
                    var clampedGuidance = Mathf.Clamp(guidanceStrength, 0f, 0.85f);
                    var chainScore = distanceSquared * (1f - (clampedGuidance * 0.45f));
                    if (chainScore < bestScore)
                    {
                        bestScore = chainScore;
                        bestDistanceSquared = distanceSquared;
                        bestCell = chainCheckpoint.Value;
                        bestType = CellType.Empty;
                        bestKind = SonarTargetKind.ChainCheckpoint;
                    }
                }
            }

            if (dangerCell.HasValue && chunks.IsInBounds(dangerCell.Value.X, dangerCell.Value.Y))
            {
                var delta = dangerCell.Value - origin;
                var distanceSquared = (delta.X * delta.X) + (delta.Y * delta.Y);
                if (distanceSquared > 0 && distanceSquared <= maxRadius * maxRadius)
                {
                    var clampedWeight = Mathf.Clamp(dangerWeight, 0f, 1f);
                    var dangerScore = distanceSquared * Mathf.Lerp(1.08f, 0.62f, clampedWeight);
                    if (dangerScore < bestScore)
                    {
                        bestScore = dangerScore;
                        bestDistanceSquared = distanceSquared;
                        bestCell = dangerCell.Value;
                        bestType = CellType.Enemy;
                        bestKind = SonarTargetKind.Danger;
                    }
                }
            }

            if (bestScore == float.MaxValue)
            {
                return new SonarReading(SonarSignalStrength.None, SonarTargetKind.None, CellType.Empty, Vector2I.Zero, 0, Vector2I.Zero);
            }

            var distance = Mathf.RoundToInt(Mathf.Sqrt(bestDistanceSquared));
            var strength = distance switch
            {
                <= 8 => SonarSignalStrength.Near,
                <= 16 => SonarSignalStrength.Medium,
                _ => SonarSignalStrength.Far
            };

            return new SonarReading(strength, bestKind, bestType, GetDirection(origin, bestCell), distance, bestCell);
        }

        private static Vector2I GetDirection(Vector2I origin, Vector2I target)
        {
            var delta = target - origin;
            return new Vector2I(Mathf.Sign(delta.X), Mathf.Sign(delta.Y));
        }
    }
}