using System.Collections.Generic;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Core
{
    public partial class ChunkManager : Node2D
    {
        private const float FlashDuration = 0.14f;
        private const float BlockFlashDuration = 0.16f;

        private readonly Dictionary<int, ChunkData> _chunks = new();
        private readonly Dictionary<long, DigFlash> _digFlashes = new();
        private readonly Dictionary<long, BlockFlash> _blockFlashes = new();
        private readonly List<int> _chunkScratch = new();
        private readonly List<long> _flashScratch = new();

        private TerrainGenerator _generator;
        private int _cameraTopRow;
        private MoveDebugInfo _movePreviewInfo;
        private bool _movePreviewVisible;
        private MoveDebugInfo _moveDebugInfo;
        private bool _moveDebugVisible;

        public const int CellSize = 16;
        public const int ChunkHeight = 16;
        public const int Columns = 67;
        public const int VisibleRows = 120;
        public const float FieldOffsetX = 4f;
        public const float FieldOffsetY = 200f;
        public const float FieldWidth = Columns * CellSize;
        public const float FieldHeight = VisibleRows * CellSize;

        public int CameraTopRow => _cameraTopRow;

        public override void _Ready()
        {
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            var needsRefresh = UpdateDigFlashes((float)delta);
            needsRefresh |= UpdateBlockFlashes((float)delta);

            if (needsRefresh)
            {
                QueueRedraw();
            }
        }

        public void Initialize()
        {
            _chunks.Clear();
            _digFlashes.Clear();
            _blockFlashes.Clear();
            _cameraTopRow = 0;
            _generator = new TerrainGenerator(unchecked((int)GD.Randi()));
            _movePreviewInfo = null;
            _movePreviewVisible = false;
            _moveDebugInfo = null;
            _moveDebugVisible = false;

            for (var chunkIndex = 0; chunkIndex < 8; chunkIndex++)
            {
                GetChunk(chunkIndex);
            }

            QueueRedraw();
        }

        public ChunkData GetChunk(int chunkIndex)
        {
            if (chunkIndex < 0)
            {
                return null;
            }

            if (_chunks.TryGetValue(chunkIndex, out var existingChunk))
            {
                return existingChunk;
            }

            var chunk = new ChunkData(chunkIndex);
            if (chunkIndex >= 2)
            {
                _generator.FillChunk(chunk);
            }

            _chunks[chunkIndex] = chunk;
            return chunk;
        }

        public bool IsInBounds(int col, int absoluteRow)
        {
            return col >= 0 && col < Columns && absoluteRow >= 0;
        }

        public byte GetCell(int col, int absoluteRow)
        {
            if (!IsInBounds(col, absoluteRow))
            {
                return (byte)CellType.Bedrock;
            }

            var chunkIndex = absoluteRow / ChunkHeight;
            var localRow = absoluteRow % ChunkHeight;
            return GetChunk(chunkIndex).GetCell(col, localRow);
        }

        public void SetCell(int col, int absoluteRow, byte value)
        {
            if (!IsInBounds(col, absoluteRow))
            {
                return;
            }

            var chunkIndex = absoluteRow / ChunkHeight;
            var localRow = absoluteRow % ChunkHeight;
            var chunk = GetChunk(chunkIndex);
            var previous = chunk.GetCell(col, localRow);
            if (previous == value)
            {
                return;
            }

            chunk.SetCell(col, localRow, value);
            if (value == (byte)CellType.Empty && previous != (byte)CellType.Empty)
            {
                _digFlashes[Pack(col, absoluteRow)] = new DigFlash((CellType)previous, FlashDuration);
            }
        }

        public void FlashBlockedCell(Vector2I cell, MoveBlockReason reason)
        {
            if (!IsInBounds(cell.X, cell.Y))
            {
                return;
            }

            _blockFlashes[Pack(cell.X, cell.Y)] = new BlockFlash(GetBlockFlashColor(reason), BlockFlashDuration);
            QueueRedraw();
        }

        public void SetMoveDebugInfo(MoveDebugInfo moveDebugInfo, bool visible)
        {
            var visibilityChanged = _moveDebugVisible != visible || _moveDebugInfo != moveDebugInfo;
            _moveDebugInfo = moveDebugInfo;
            _moveDebugVisible = visible;

            if (visible || visibilityChanged)
            {
                QueueRedraw();
            }
        }

        public void SetMovePreview(MoveDebugInfo movePreviewInfo, bool visible)
        {
            var visibilityChanged = _movePreviewVisible != visible || _movePreviewInfo != movePreviewInfo;
            _movePreviewInfo = movePreviewInfo;
            _movePreviewVisible = visible;

            if (visible || visibilityChanged)
            {
                QueueRedraw();
            }
        }

        public void UpdateCamera(int playerRow)
        {
            var newTopRow = Mathf.Max(0, playerRow - (VisibleRows / 2));
            var changed = newTopRow != _cameraTopRow;
            _cameraTopRow = newTopRow;

            var minChunk = Mathf.Max(0, _cameraTopRow / ChunkHeight - 1);
            var maxChunk = ((_cameraTopRow + VisibleRows - 1) / ChunkHeight) + 1;

            for (var chunkIndex = minChunk; chunkIndex <= maxChunk; chunkIndex++)
            {
                GetChunk(chunkIndex);
            }

            _chunkScratch.Clear();
            foreach (var chunkIndex in _chunks.Keys)
            {
                if (chunkIndex < minChunk - 1 || chunkIndex > maxChunk + 1)
                {
                    _chunkScratch.Add(chunkIndex);
                }
            }

            for (var index = 0; index < _chunkScratch.Count; index++)
            {
                _chunks.Remove(_chunkScratch[index]);
                changed = true;
            }

            if (changed)
            {
                QueueRedraw();
            }
        }

        public Vector2 GridToScreen(int col, int row)
        {
            return new Vector2(FieldOffsetX + col * CellSize, FieldOffsetY + (row - _cameraTopRow) * CellSize);
        }

        public Vector2 GridToWorld(int col, int row)
        {
            return new Vector2(FieldOffsetX + col * CellSize, FieldOffsetY + row * CellSize);
        }

        public Vector2 GridToWorldCenter(int col, int row)
        {
            return GridToWorld(col, row) + new Vector2(CellSize * 0.5f, CellSize * 0.5f);
        }

        public void Reset()
        {
            Initialize();
        }

        public void RequestRefresh()
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_generator == null)
            {
                return;
            }

            var backgroundTopLeft = GridToWorld(0, _cameraTopRow);
            DrawRect(new Rect2(backgroundTopLeft.X, backgroundTopLeft.Y, FieldWidth, FieldHeight), new Color(0.08f, 0.08f, 0.10f));

            for (var row = _cameraTopRow; row < _cameraTopRow + VisibleRows; row++)
            {
                var depthTier = TerrainGenerator.GetDepthTier(row);
                var runType = CellType.Empty;
                var runStart = 0;

                for (var col = 0; col <= Columns; col++)
                {
                    var currentType = col < Columns ? (CellType)GetCell(col, row) : CellType.Empty;
                    if (col == 0)
                    {
                        runType = currentType;
                        runStart = 0;
                        continue;
                    }

                    if (currentType == runType)
                    {
                        continue;
                    }

                    if (runType != CellType.Empty)
                    {
                        DrawRun(row, runStart, col - runStart, runType, depthTier);
                    }

                    runType = currentType;
                    runStart = col;
                }
            }

            DrawSpecialMarkers();
            DrawTerrainAccents();
            DrawMovePreview();
            DrawDigFlashes();
            DrawBlockFlashes();
            DrawMoveDebugOverlay();
        }

        private void DrawSpecialMarkers()
        {
            for (var row = _cameraTopRow; row < _cameraTopRow + VisibleRows; row++)
            {
                for (var col = 0; col < Columns; col++)
                {
                    var type = (CellType)GetCell(col, row);
                    if (type != CellType.RecoveryPoint && type != CellType.Ore)
                    {
                        continue;
                    }

                    var rect = new Rect2(GridToWorld(col, row), new Vector2(CellSize, CellSize));
                    if (type == CellType.RecoveryPoint)
                    {
                        DrawCircle(rect.GetCenter(), CellSize * 0.28f, new Color(0.92f, 1f, 0.98f, 0.9f));
                        DrawArc(rect.GetCenter(), CellSize * 0.36f, 0f, Mathf.Tau, 24, new Color(0.12f, 0.48f, 0.52f, 0.95f), 2f);
                    }
                    else
                    {
                        DrawLine(rect.Position + new Vector2(3f, 3f), rect.End - new Vector2(3f, 3f), new Color(1f, 0.95f, 0.72f, 0.72f), 1.4f);
                    }
                }
            }
        }

        private void DrawTerrainAccents()
        {
            for (var row = _cameraTopRow; row < _cameraTopRow + VisibleRows; row++)
            {
                for (var col = 0; col < Columns; col++)
                {
                    var type = (CellType)GetCell(col, row);
                    if (type == CellType.Empty)
                    {
                        continue;
                    }

                    DrawCellAccent(col, row, type);
                }
            }
        }

        private void DrawCellAccent(int col, int row, CellType type)
        {
            var rect = new Rect2(GridToWorld(col, row), new Vector2(CellSize, CellSize));
            switch (type)
            {
                case CellType.Dirt:
                    DrawLine(rect.Position + new Vector2(2f, 4f), rect.Position + new Vector2(CellSize - 2f, 4f), new Color(1f, 0.93f, 0.72f, 0.18f), 1.1f);
                    DrawLine(rect.Position + new Vector2(3f, CellSize - 4f), rect.Position + new Vector2(CellSize - 3f, CellSize - 5f), new Color(0.33f, 0.21f, 0.11f, 0.24f), 1f);
                    break;
                case CellType.Stone:
                    DrawLine(rect.Position + new Vector2(2f, 5f), rect.Position + new Vector2(CellSize - 2f, 5f), new Color(0.95f, 0.98f, 1f, 0.22f), 1.1f);
                    DrawLine(rect.Position + new Vector2(4f, 10f), rect.Position + new Vector2(CellSize - 4f, 10f), new Color(0.08f, 0.10f, 0.14f, 0.34f), 1f);
                    break;
                case CellType.HardRock:
                    DrawRect(rect.Grow(-1.5f), new Color(0.82f, 0.90f, 1f, 0.34f), false, 1.2f);
                    DrawLine(rect.Position + new Vector2(3f, 3f), rect.Position + new Vector2(CellSize - 3f, CellSize - 3f), new Color(0.94f, 0.98f, 1f, 0.18f), 1f);
                    DrawLine(rect.Position + new Vector2(CellSize - 3f, 3f), rect.Position + new Vector2(3f, CellSize - 3f), new Color(0.03f, 0.04f, 0.06f, 0.28f), 1f);
                    break;
                case CellType.Bedrock:
                    DrawRect(rect, new Color(0.92f, 0.74f, 0.96f, 0.24f), false, 1.8f);
                    DrawRect(rect.Grow(-4f), new Color(1f, 0.94f, 1f, 0.16f), false, 1f);
                    break;
                case CellType.Ore:
                    DrawLine(rect.Position + new Vector2(3f, CellSize * 0.5f), rect.Position + new Vector2(CellSize - 3f, CellSize * 0.5f), new Color(1f, 0.96f, 0.72f, 0.36f), 1.2f);
                    DrawLine(rect.Position + new Vector2(CellSize * 0.5f, 3f), rect.Position + new Vector2(CellSize * 0.5f, CellSize - 3f), new Color(1f, 0.96f, 0.72f, 0.32f), 1.2f);
                    DrawArc(rect.GetCenter(), CellSize * 0.22f, 0f, Mathf.Tau, 18, new Color(1f, 0.98f, 0.76f, 0.3f), 1.2f);
                    break;
                case CellType.RecoveryPoint:
                    DrawRect(rect.Grow(-1f), new Color(0.82f, 1f, 0.94f, 0.24f), false, 1.1f);
                    break;
                case CellType.Item:
                    DrawLine(rect.Position + new Vector2(3f, 3f), rect.End - new Vector2(3f, 3f), new Color(1f, 0.84f, 0.56f, 0.26f), 1.1f);
                    DrawLine(rect.Position + new Vector2(CellSize - 3f, 3f), rect.Position + new Vector2(3f, CellSize - 3f), new Color(1f, 0.84f, 0.56f, 0.22f), 1.1f);
                    break;
                case CellType.Enemy:
                case CellType.Boss:
                    DrawRect(rect.Grow(-1f), new Color(1f, 0.58f, 0.58f, 0.24f), false, 1.2f);
                    break;
            }
        }

        private void DrawMovePreview()
        {
            if (!_movePreviewVisible || _movePreviewInfo == null || !_movePreviewInfo.HasTarget)
            {
                return;
            }

            for (var index = 0; index < _movePreviewInfo.DigArea.Count; index++)
            {
                var cell = _movePreviewInfo.DigArea[index];
                var type = GetPreviewCellType(cell);
                DrawPreviewCell(cell, GetPreviewFillColor(type), GetPreviewOutlineColor(type), 1.3f);
            }

            for (var index = 0; index < _movePreviewInfo.OccupancyArea.Count; index++)
            {
                var cell = _movePreviewInfo.OccupancyArea[index];
                if (ContainsPreviewCell(_movePreviewInfo.DigArea, cell))
                {
                    continue;
                }

                DrawDebugCell(cell, new Color(0f, 0f, 0f, 0f), _movePreviewInfo.CanMove
                    ? new Color(0.96f, 0.98f, 1f, 0.72f)
                    : new Color(1f, 0.54f, 0.54f, 0.9f), 1.6f);
            }

            DrawDebugCell(_movePreviewInfo.Target, new Color(1f, 1f, 1f, 0.06f), new Color(1f, 1f, 1f, 0.92f), 1.8f);

            if (_movePreviewInfo.HasBlockedCell)
            {
                DrawDebugCell(_movePreviewInfo.BlockedCell, new Color(1f, 0.24f, 0.24f, 0.20f), new Color(1f, 0.54f, 0.54f, 1f), 2f);
            }
        }

        private CellType GetPreviewCellType(Vector2I cell)
        {
            return IsInBounds(cell.X, cell.Y) ? (CellType)GetCell(cell.X, cell.Y) : CellType.Bedrock;
        }

        private static Color GetPreviewFillColor(CellType type)
        {
            if (!CellTypeUtil.IsDiggable(type))
            {
                return new Color(1f, 0.28f, 0.28f, 0.22f);
            }

            if (CellTypeUtil.IsHardDig(type))
            {
                return new Color(1f, 0.82f, 0.24f, 0.20f);
            }

            return new Color(0.36f, 0.98f, 0.62f, 0.18f);
        }

        private static Color GetPreviewOutlineColor(CellType type)
        {
            if (!CellTypeUtil.IsDiggable(type))
            {
                return new Color(1f, 0.50f, 0.50f, 0.92f);
            }

            if (CellTypeUtil.IsHardDig(type))
            {
                return new Color(1f, 0.90f, 0.48f, 0.94f);
            }

            return new Color(0.72f, 1f, 0.82f, 0.88f);
        }

        private void DrawPreviewCell(Vector2I cell, Color fillColor, Color outlineColor, float outlineWidth)
        {
            DrawDebugCell(cell, fillColor, outlineColor, outlineWidth);
        }

        private static bool ContainsPreviewCell(IReadOnlyList<Vector2I> cells, Vector2I target)
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

        private bool UpdateDigFlashes(float delta)
        {
            if (_digFlashes.Count == 0)
            {
                return false;
            }

            _flashScratch.Clear();
            foreach (var key in _digFlashes.Keys)
            {
                _flashScratch.Add(key);
            }

            var needsRefresh = false;
            for (var index = 0; index < _flashScratch.Count; index++)
            {
                var key = _flashScratch[index];
                var flash = _digFlashes[key];
                flash.TimeLeft -= delta;
                if (flash.TimeLeft <= 0f)
                {
                    _digFlashes.Remove(key);
                }
                else
                {
                    _digFlashes[key] = flash;
                    needsRefresh = true;
                }
            }

            return needsRefresh;
        }

        private bool UpdateBlockFlashes(float delta)
        {
            if (_blockFlashes.Count == 0)
            {
                return false;
            }

            _flashScratch.Clear();
            foreach (var key in _blockFlashes.Keys)
            {
                _flashScratch.Add(key);
            }

            var needsRefresh = false;
            for (var index = 0; index < _flashScratch.Count; index++)
            {
                var key = _flashScratch[index];
                var flash = _blockFlashes[key];
                flash.TimeLeft -= delta;
                if (flash.TimeLeft <= 0f)
                {
                    _blockFlashes.Remove(key);
                }
                else
                {
                    _blockFlashes[key] = flash;
                    needsRefresh = true;
                }
            }

            return needsRefresh;
        }

        private void DrawDigFlashes()
        {
            foreach (var entry in _digFlashes)
            {
                Unpack(entry.Key, out var col, out var row);
                if (row < _cameraTopRow || row >= _cameraTopRow + VisibleRows)
                {
                    continue;
                }

                var rect = new Rect2(GridToWorld(col, row), new Vector2(CellSize, CellSize));
                var color = CellTypeUtil.GetColor(entry.Value.Type, TerrainGenerator.GetDepthTier(row));
                color.A = Mathf.Clamp(entry.Value.TimeLeft / FlashDuration, 0f, 1f) * 0.55f;
                DrawRect(rect, color);
            }
        }

        private void DrawBlockFlashes()
        {
            foreach (var entry in _blockFlashes)
            {
                Unpack(entry.Key, out var col, out var row);
                if (row < _cameraTopRow || row >= _cameraTopRow + VisibleRows)
                {
                    continue;
                }

                var rect = new Rect2(GridToWorld(col, row), new Vector2(CellSize, CellSize));
                var color = entry.Value.Color;
                color.A *= Mathf.Clamp(entry.Value.TimeLeft / BlockFlashDuration, 0f, 1f);
                DrawRect(rect, color);
                DrawRect(rect, new Color(1f, 0.96f, 0.96f, color.A), false, 1.4f);
            }
        }

        private void DrawMoveDebugOverlay()
        {
            if (!_moveDebugVisible || _moveDebugInfo == null || !_moveDebugInfo.HasTarget)
            {
                return;
            }

            for (var index = 0; index < _moveDebugInfo.DigArea.Count; index++)
            {
                DrawDebugCell(_moveDebugInfo.DigArea[index], new Color(0.20f, 0.74f, 0.95f, 0.12f), new Color(0.45f, 0.88f, 1f, 0.85f), 1.2f);
            }

            for (var index = 0; index < _moveDebugInfo.OccupancyArea.Count; index++)
            {
                DrawDebugCell(_moveDebugInfo.OccupancyArea[index], new Color(0f, 0f, 0f, 0f), new Color(1f, 0.82f, 0.24f, 0.92f), 1.4f);
            }

            DrawDebugCell(_moveDebugInfo.Target, new Color(1f, 1f, 1f, 0.08f), new Color(1f, 1f, 1f, 0.95f), 1.8f);

            if (_moveDebugInfo.HasBlockedCell)
            {
                DrawDebugCell(_moveDebugInfo.BlockedCell, new Color(1f, 0.20f, 0.20f, 0.18f), new Color(1f, 0.46f, 0.46f, 1f), 2f);
            }
        }

        private void DrawDebugCell(Vector2I cell, Color fillColor, Color outlineColor, float outlineWidth)
        {
            if (cell.Y < _cameraTopRow || cell.Y >= _cameraTopRow + VisibleRows || !IsInBounds(cell.X, cell.Y))
            {
                return;
            }

            var rect = new Rect2(GridToWorld(cell.X, cell.Y), new Vector2(CellSize, CellSize));
            if (fillColor.A > 0f)
            {
                DrawRect(rect, fillColor);
            }

            DrawRect(rect, outlineColor, false, outlineWidth);
        }

        private void DrawRun(int row, int startCol, int length, CellType type, int depthTier)
        {
            var rect = new Rect2(GridToWorld(startCol, row), new Vector2(length * CellSize, CellSize));
            DrawRect(rect, CellTypeUtil.GetColor(type, depthTier));
        }

        private static Color GetBlockFlashColor(MoveBlockReason reason)
        {
            return reason switch
            {
                MoveBlockReason.Occupancy => new Color(1f, 0.82f, 0.24f, 0.72f),
                MoveBlockReason.OutOfBounds => new Color(1f, 0.44f, 0.30f, 0.78f),
                _ => new Color(1f, 0.22f, 0.22f, 0.78f)
            };
        }

        private static long Pack(int col, int row)
        {
            return ((long)row << 32) | (uint)col;
        }

        private static void Unpack(long value, out int col, out int row)
        {
            col = (int)(value & 0xffffffff);
            row = (int)(value >> 32);
        }

        private struct DigFlash
        {
            public DigFlash(CellType type, float timeLeft)
            {
                Type = type;
                TimeLeft = timeLeft;
            }

            public CellType Type;

            public float TimeLeft;
        }

        private struct BlockFlash
        {
            public BlockFlash(Color color, float timeLeft)
            {
                Color = color;
                TimeLeft = timeLeft;
            }

            public Color Color;

            public float TimeLeft;
        }
    }
}