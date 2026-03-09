using System.Collections.Generic;
using Godot;

namespace CursedBlood.Core
{
    public enum DigShape : byte
    {
        Square,
        Diamond,
        Fan
    }

    public static class DigHelper
    {
        public static List<Vector2I> GetDigArea(Vector2I playerPos, Vector2I direction, int width, DigShape shape, int playerSize)
        {
            var result = new List<Vector2I>(24);
            FillDigArea(result, playerPos, direction, width, shape, playerSize);
            return result;
        }

        public static void FillDigArea(List<Vector2I> buffer, Vector2I playerPos, Vector2I direction, int width, DigShape shape, int playerSize)
        {
            buffer.Clear();
            if (direction == Vector2I.Zero)
            {
                return;
            }

            if (shape != DigShape.Square)
            {
                shape = DigShape.Square;
            }

            var effectiveWidth = Mathf.Max(width, playerSize);
            AddOccupancyDifference(buffer, playerPos, playerPos + direction, playerSize);
            AddFrontWidth(buffer, playerPos, direction, effectiveWidth, playerSize);
        }

        public static List<Vector2I> GetCenteredArea(Vector2I center, int size)
        {
            var result = new List<Vector2I>(size * size);
            FillCenteredArea(result, center, size);
            return result;
        }

        public static void FillCenteredArea(List<Vector2I> buffer, Vector2I center, int size)
        {
            buffer.Clear();
            var half = size / 2;
            for (var row = center.Y - half; row <= center.Y + half; row++)
            {
                for (var col = center.X - half; col <= center.X + half; col++)
                {
                    buffer.Add(new Vector2I(col, row));
                }
            }
        }

        public static int ExecuteDig(ChunkManager chunks, IReadOnlyList<Vector2I> area)
        {
            var dugCount = 0;
            for (var index = 0; index < area.Count; index++)
            {
                var cell = area[index];
                var type = (CellType)chunks.GetCell(cell.X, cell.Y);
                if (type == CellType.Empty || type == CellType.RecoveryPoint || type == CellType.Item || !CellTypeUtil.IsDiggable(type))
                {
                    continue;
                }

                chunks.SetCell(cell.X, cell.Y, (byte)CellType.Empty);
                dugCount++;
            }

            if (dugCount > 0)
            {
                chunks.RequestRefresh();
            }

            return dugCount;
        }

        private static void AddOccupancyDifference(List<Vector2I> buffer, Vector2I currentCenter, Vector2I targetCenter, int playerSize)
        {
            var half = playerSize / 2;
            for (var row = targetCenter.Y - half; row <= targetCenter.Y + half; row++)
            {
                for (var col = targetCenter.X - half; col <= targetCenter.X + half; col++)
                {
                    if (col >= currentCenter.X - half && col <= currentCenter.X + half && row >= currentCenter.Y - half && row <= currentCenter.Y + half)
                    {
                        continue;
                    }

                    AddUnique(buffer, new Vector2I(col, row));
                }
            }
        }

        private static void AddFrontWidth(List<Vector2I> buffer, Vector2I playerPos, Vector2I direction, int width, int playerSize)
        {
            var halfWidth = width / 2;
            var frontOffset = playerSize / 2 + 1;
            var frontCenter = playerPos + new Vector2I(direction.X * frontOffset, direction.Y * frontOffset);
            var lateral = GetLateralVector(direction);

            for (var offset = -halfWidth; offset <= halfWidth; offset++)
            {
                var cell = new Vector2I(frontCenter.X + lateral.X * offset, frontCenter.Y + lateral.Y * offset);
                AddUnique(buffer, cell);
            }
        }

        private static Vector2I GetLateralVector(Vector2I direction)
        {
            if (direction.X != 0 && direction.Y == 0)
            {
                return Vector2I.Down;
            }

            if (direction.Y != 0 && direction.X == 0)
            {
                return Vector2I.Right;
            }

            return direction switch
            {
                { X: 1, Y: 1 } => new Vector2I(1, -1),
                { X: -1, Y: -1 } => new Vector2I(1, -1),
                { X: 1, Y: -1 } => new Vector2I(1, 1),
                { X: -1, Y: 1 } => new Vector2I(1, 1),
                _ => Vector2I.Right
            };
        }

        private static void AddUnique(List<Vector2I> buffer, Vector2I cell)
        {
            for (var index = 0; index < buffer.Count; index++)
            {
                if (buffer[index] == cell)
                {
                    return;
                }
            }

            buffer.Add(cell);
        }
    }
}