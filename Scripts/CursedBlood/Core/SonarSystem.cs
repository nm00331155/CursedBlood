using Godot;

namespace CursedBlood.Core
{
    public enum SonarSignalStrength
    {
        None,
        Far,
        Medium,
        Near
    }

    public readonly record struct SonarReading(SonarSignalStrength Strength, CellType TargetType, Vector2I Direction, int DistanceCells)
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
                SonarSignalStrength.Far => $"ソナー: 微反応 / {DistanceCells}m先",
                SonarSignalStrength.Medium => $"ソナー: {directionLabel} / {DistanceCells}m",
                SonarSignalStrength.Near => $"ソナー: {directionLabel} / {DescribeTarget(TargetType)} / {DistanceCells}m",
                _ => "ソナー: 反応なし"
            };
        }

        private static string DescribeTarget(CellType type)
        {
            return type switch
            {
                CellType.RecoveryPoint => "回収ポイント",
                CellType.Ore => "鉱石",
                CellType.Item => "アイテム",
                CellType.Enemy => "敵影",
                _ => "反応源"
            };
        }
    }

    public sealed class SonarSystem
    {
        private const float ScanInterval = 0.15f;
        private const int MaxRadius = 28;

        private float _scanTimer;

        public SonarReading CurrentReading { get; private set; } = new(SonarSignalStrength.None, CellType.Empty, Vector2I.Zero, 0);

        public void Reset()
        {
            _scanTimer = 0f;
            CurrentReading = new SonarReading(SonarSignalStrength.None, CellType.Empty, Vector2I.Zero, 0);
        }

        public void Update(float delta, ChunkManager chunks, Vector2I origin)
        {
            _scanTimer -= delta;
            if (_scanTimer > 0f)
            {
                return;
            }

            _scanTimer = ScanInterval;
            CurrentReading = Scan(chunks, origin);
        }

        private static SonarReading Scan(ChunkManager chunks, Vector2I origin)
        {
            var bestDistanceSquared = int.MaxValue;
            var bestCell = Vector2I.Zero;
            var bestType = CellType.Empty;

            for (var row = origin.Y - MaxRadius; row <= origin.Y + MaxRadius; row++)
            {
                for (var col = origin.X - MaxRadius; col <= origin.X + MaxRadius; col++)
                {
                    if (!chunks.IsInBounds(col, row))
                    {
                        continue;
                    }

                    var type = (CellType)chunks.GetCell(col, row);
                    if (type is not (CellType.RecoveryPoint or CellType.Ore or CellType.Item or CellType.Enemy))
                    {
                        continue;
                    }

                    var dx = col - origin.X;
                    var dy = row - origin.Y;
                    var distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared == 0 || distanceSquared >= bestDistanceSquared || distanceSquared > MaxRadius * MaxRadius)
                    {
                        continue;
                    }

                    bestDistanceSquared = distanceSquared;
                    bestCell = new Vector2I(col, row);
                    bestType = type;
                }
            }

            if (bestDistanceSquared == int.MaxValue)
            {
                return new SonarReading(SonarSignalStrength.None, CellType.Empty, Vector2I.Zero, 0);
            }

            var distance = Mathf.RoundToInt(Mathf.Sqrt(bestDistanceSquared));
            var strength = distance switch
            {
                <= 8 => SonarSignalStrength.Near,
                <= 16 => SonarSignalStrength.Medium,
                _ => SonarSignalStrength.Far
            };

            return new SonarReading(strength, bestType, GetDirection(origin, bestCell), distance);
        }

        private static Vector2I GetDirection(Vector2I origin, Vector2I target)
        {
            var delta = target - origin;
            return new Vector2I(Mathf.Sign(delta.X), Mathf.Sign(delta.Y));
        }
    }
}