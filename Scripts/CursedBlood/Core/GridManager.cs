using System;
using System.Collections.Generic;
using CursedBlood.Enemy;
using Godot;

namespace CursedBlood.Core
{
    public partial class GridManager : Node2D
    {
        public const int Columns = 7;
        public const int VisibleRows = 9;
        public const float CellSize = 140f;
        public const float GridOffsetX = 50f;
        public const float GridOffsetY = 200f;
        private const float ScreenWidth = 1080f;
        private const float ScreenHeight = 1920f;

        private readonly Dictionary<int, CellData[]> _rows = new();
        private int _topVisibleRow;
        private int _maxGeneratedRow = -1;
        private GameTheme _theme = ThemeSettings.CreateDefault().BuildTheme();
        private GridGenerationContext _generationContext = new();

        public int TopVisibleRow => _topVisibleRow;

        public int BottomVisibleRow => _topVisibleRow + VisibleRows;

        public override void _Ready()
        {
            InitializeGrid();
        }

        public void Configure(GridGenerationContext generationContext)
        {
            _generationContext = generationContext ?? new GridGenerationContext();
        }

        public void InitializeGrid()
        {
            _rows.Clear();
            _topVisibleRow = 0;
            _maxGeneratedRow = -1;

            for (var row = 0; row < VisibleRows + 4; row++)
            {
                GenerateAndStoreRow(row);
            }

            QueueRedraw();
        }

        public void ApplyTheme(GameTheme theme)
        {
            _theme = theme;
            QueueRedraw();
        }

        public IEnumerable<CellData> EnumerateCells(int startRow, int endRow)
        {
            for (var row = startRow; row <= endRow; row++)
            {
                if (!_rows.TryGetValue(row, out var rowData))
                {
                    continue;
                }

                foreach (var cell in rowData)
                {
                    yield return cell;
                }
            }
        }

        public IEnumerable<CellData> EnumerateVisibleCells()
        {
            return EnumerateCells(_topVisibleRow - 1, _topVisibleRow + VisibleRows + 2);
        }

        public CellData GetCell(int column, int row)
        {
            if (_rows.TryGetValue(row, out var rowData) && column >= 0 && column < Columns)
            {
                return rowData[column];
            }

            return null;
        }

        public void UpdateVisibleRange(int playerRow)
        {
            var newTopRow = Mathf.Max(0, playerRow - 4);
            _topVisibleRow = newTopRow;

            var needUntil = _topVisibleRow + VisibleRows + 4;
            while (_maxGeneratedRow < needUntil)
            {
                GenerateAndStoreRow(_maxGeneratedRow + 1);
            }

            var keysToRemove = new List<int>();
            foreach (var key in _rows.Keys)
            {
                if (key < _topVisibleRow - 2)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _rows.Remove(key);
            }

            QueueRedraw();
        }

        public Vector2 GridToWorld(int column, int row)
        {
            var x = GridOffsetX + column * CellSize + CellSize / 2f;
            var y = GridOffsetY + (row - _topVisibleRow) * CellSize + CellSize / 2f;
            return new Vector2(x, y);
        }

        public Vector2 GridToWorldAbsolute(int column, int row)
        {
            var x = GridOffsetX + column * CellSize + CellSize / 2f;
            var y = GridOffsetY + row * CellSize + CellSize / 2f;
            return new Vector2(x, y);
        }

        public void QueueRefresh()
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawRect(new Rect2(0f, 0f, ScreenWidth, ScreenHeight), _theme.BackgroundColor);

            for (var row = _topVisibleRow; row < _topVisibleRow + VisibleRows + 1; row++)
            {
                if (!_rows.TryGetValue(row, out var rowData))
                {
                    continue;
                }

                for (var column = 0; column < Columns; column++)
                {
                    var cell = rowData[column];
                    var x = GridOffsetX + column * CellSize;
                    var y = GridOffsetY + (row - _topVisibleRow) * CellSize;
                    var rect = new Rect2(x, y, CellSize, CellSize);

                    var backgroundColor = cell.Type switch
                    {
                        CellType.Empty => _theme.EmptyCellColor,
                        CellType.Normal => _theme.NormalCellColor,
                        CellType.Hard => _theme.HardCellColor,
                        CellType.Ore => _theme.NormalCellColor.Lerp(new Color(1f, 0.82f, 0.18f), 0.45f),
                        CellType.Enemy => _theme.EmptyCellColor.Lerp(new Color(0.95f, 0.2f, 0.2f), 0.2f),
                        CellType.Indestructible => _theme.IndestructibleColor,
                        CellType.Boss => new Color(0.35f, 0.10f, 0.14f),
                        _ => _theme.NormalCellColor
                    };

                    DrawRect(rect, backgroundColor);

                    if (cell.Type == CellType.Hard)
                    {
                        var center = new Vector2(x + CellSize / 2f, y + CellSize / 2f);
                        var size = CellSize * 0.25f;
                        var markColor = _theme.BorderColor.Lerp(_theme.TextColor, 0.35f);
                        DrawLine(center - new Vector2(size, size), center + new Vector2(size, size), markColor, 3f);
                        DrawLine(center - new Vector2(-size, size), center + new Vector2(-size, size), markColor, 3f);
                    }

                    if (cell.Type == CellType.Indestructible)
                    {
                        for (var offset = 0f; offset < CellSize; offset += 12f)
                        {
                            DrawLine(new Vector2(x + offset, y), new Vector2(x, y + offset), _theme.BorderColor, 1.4f);
                        }
                    }

                    if (cell.Type == CellType.Ore)
                    {
                        DrawCircle(rect.GetCenter(), 24f, new Color(1f, 0.88f, 0.20f));
                        DrawArc(rect.GetCenter(), 30f, 0f, Mathf.Tau, 24, new Color(1f, 0.96f, 0.55f), 2f);
                    }

                    if (cell.Type == CellType.Enemy && cell.Enemy != null)
                    {
                        DrawEnemyMarker(rect, cell.Enemy, cell.GridPosition.Y);
                    }

                    if (cell.Type == CellType.Boss && cell.BossCell != null)
                    {
                        var inset = rect.Grow(-14f);
                        DrawRect(inset, new Color(0.6f, 0.12f, 0.18f), false, 4f);
                        DrawLine(inset.Position, inset.End, new Color(1f, 0.55f, 0.55f), 2f);
                        DrawLine(new Vector2(inset.End.X, inset.Position.Y), new Vector2(inset.Position.X, inset.End.Y), new Color(1f, 0.55f, 0.55f), 2f);
                    }

                    if (cell.HasDrop)
                    {
                        DrawDroppedItemMarker(rect, cell.DroppedItem);
                    }

                    DrawRect(rect, _theme.GridLineColor, false, 1f);
                }
            }
        }

        public void Reset()
        {
            InitializeGrid();
        }

        private void GenerateAndStoreRow(int rowIndex)
        {
            var row = rowIndex <= 1
                ? GridGenerator.GenerateStartRow(rowIndex, _generationContext)
                : GridGenerator.GenerateRow(rowIndex, _generationContext);

            _rows[rowIndex] = row;
            if (rowIndex > _maxGeneratedRow)
            {
                _maxGeneratedRow = rowIndex;
            }
        }

        private void DrawEnemyMarker(Rect2 rect, EnemyData enemy, int depth)
        {
            var center = rect.GetCenter();
            var color = enemy.GetColor(GridGenerator.GetDepthTier(depth));
            switch (enemy.Type)
            {
                case EnemyType.Slime:
                    DrawCircle(center, 26f, color);
                    break;
                case EnemyType.Shooter:
                    DrawRect(new Rect2(center.X - 24f, center.Y - 24f, 48f, 48f), color);
                    DrawLine(center + new Vector2(-20f, 0f), center + new Vector2(20f, 0f), Colors.White, 2f);
                    break;
                case EnemyType.Spreader:
                    DrawCircle(center, 24f, color);
                    DrawLine(center + new Vector2(-22f, 0f), center + new Vector2(22f, 0f), Colors.White, 2f);
                    DrawLine(center + new Vector2(0f, -22f), center + new Vector2(0f, 22f), Colors.White, 2f);
                    break;
                case EnemyType.Bomber:
                    DrawCircle(center, 24f, color);
                    DrawLine(center + new Vector2(8f, -18f), center + new Vector2(22f, -30f), new Color(1f, 0.8f, 0.4f), 3f);
                    break;
                case EnemyType.Collector:
                    DrawRect(new Rect2(center.X - 26f, center.Y - 26f, 52f, 52f), color);
                    DrawLine(center + new Vector2(-20f, -20f), center + new Vector2(20f, 20f), Colors.Black, 3f);
                    DrawLine(center + new Vector2(20f, -20f), center + new Vector2(-20f, 20f), Colors.Black, 3f);
                    break;
            }
        }

        private void DrawDroppedItemMarker(Rect2 rect, Equipment.DroppedItem droppedItem)
        {
            var center = rect.GetCenter();
            var color = droppedItem.GetColor();
            var top = center + new Vector2(0f, -18f);
            var right = center + new Vector2(18f, 0f);
            var bottom = center + new Vector2(0f, 18f);
            var left = center + new Vector2(-18f, 0f);
            DrawLine(top, right, color, 3f);
            DrawLine(right, bottom, color, 3f);
            DrawLine(bottom, left, color, 3f);
            DrawLine(left, top, color, 3f);
        }
    }
}