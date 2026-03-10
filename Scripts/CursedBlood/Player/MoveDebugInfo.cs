using System.Collections.Generic;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Player
{
    public enum MoveBlockReason
    {
        None,
        Bedrock,
        Occupancy,
        OutOfBounds
    }

    public sealed class MoveDebugInfo
    {
        private readonly List<Vector2I> _digArea = new(32);
        private readonly List<Vector2I> _occupancyArea = new(25);

        public Vector2I Origin { get; private set; }

        public Vector2I Direction { get; private set; }

        public Vector2I Target { get; private set; }

        public CellType TargetCellType { get; private set; } = CellType.Empty;

        public CellType SlowestCellType { get; private set; } = CellType.Empty;

        public Vector2I BlockedCell { get; private set; } = Vector2I.Zero;

        public CellType BlockedCellType { get; private set; } = CellType.Empty;

        public MoveBlockReason BlockReason { get; private set; } = MoveBlockReason.None;

        public float MaxHardness { get; private set; } = 1f;

        public bool RequiresDig { get; private set; }

        public bool CanMove { get; private set; }

        public bool HasTarget => Direction != Vector2I.Zero;

        public bool HasBlockedCell { get; private set; }

        public IReadOnlyList<Vector2I> DigArea => _digArea;

        public IReadOnlyList<Vector2I> OccupancyArea => _occupancyArea;

        public void Reset(Vector2I origin, Vector2I direction)
        {
            Origin = origin;
            Direction = direction;
            Target = origin + direction;
            TargetCellType = CellType.Empty;
            SlowestCellType = CellType.Empty;
            BlockedCell = Vector2I.Zero;
            BlockedCellType = CellType.Empty;
            BlockReason = MoveBlockReason.None;
            MaxHardness = 1f;
            RequiresDig = false;
            CanMove = false;
            HasBlockedCell = false;
            _digArea.Clear();
            _occupancyArea.Clear();
        }

        public void SetDigArea(IReadOnlyList<Vector2I> area)
        {
            _digArea.Clear();
            for (var index = 0; index < area.Count; index++)
            {
                _digArea.Add(area[index]);
            }
        }

        public void SetOccupancyArea(IReadOnlyList<Vector2I> area)
        {
            _occupancyArea.Clear();
            for (var index = 0; index < area.Count; index++)
            {
                _occupancyArea.Add(area[index]);
            }
        }

        public void SetTargetCellType(CellType type)
        {
            TargetCellType = type;
        }

        public void SetRequiresDig(bool requiresDig)
        {
            RequiresDig = requiresDig;
        }

        public void ConsiderHardness(CellType type)
        {
            var hardness = CellTypeUtil.GetHardness(type);
            if (hardness < MaxHardness)
            {
                return;
            }

            MaxHardness = hardness;
            SlowestCellType = type;
        }

        public void AllowMove()
        {
            CanMove = HasTarget;
            BlockReason = MoveBlockReason.None;
            HasBlockedCell = false;
        }

        public void Block(MoveBlockReason reason, Vector2I cell, CellType type)
        {
            CanMove = false;
            BlockReason = reason;
            BlockedCell = cell;
            BlockedCellType = type;
            HasBlockedCell = true;
        }
    }
}