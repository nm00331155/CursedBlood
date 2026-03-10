using System.Collections.Generic;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Core
{
    public partial class ChunkManager : Node2D
    {
        private const float FlashDuration = 0.14f;
        private const float BlockFlashDuration = 0.16f;

        private readonly Dictionary<Vector2I, ChunkData> _chunks = new();
        private readonly Dictionary<long, byte> _cellOverrides = new();
        private readonly HashSet<long> _exploredCells = new();
        private readonly Dictionary<long, DigFlash> _digFlashes = new();
        private readonly Dictionary<long, BlockFlash> _blockFlashes = new();
        private readonly List<Vector2I> _chunkScratch = new();
        private readonly List<long> _flashScratch = new();

        private TerrainGenerator _generator;
        private ChainVisualState _chainVisualState;
        private float _markerPulseTime;
        private int _cameraTopRow;
        private int _cameraLeftCol;
        private MoveDebugInfo _movePreviewInfo;
        private bool _movePreviewVisible;
        private MoveDebugInfo _moveDebugInfo;
        private bool _moveDebugVisible;

        public const int CellSize = 16;
        public const int VisibleColumns = 84;
        public const int VisibleRows = 108;
        public const float WorldOriginX = 532f;
        public const float WorldOriginY = 0f;

        public int CameraTopRow => _cameraTopRow;

        public override void _Ready()
        {
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            var needsRefresh = UpdateDigFlashes((float)delta);
            needsRefresh |= UpdateBlockFlashes((float)delta);
            _markerPulseTime += (float)delta;
            needsRefresh |= _chainVisualState.HasCheckpoint;

            if (needsRefresh)
            {
                QueueRedraw();
            }
        }

        public void Initialize()
        {
            _chunks.Clear();
            _cellOverrides.Clear();
            _exploredCells.Clear();
            _digFlashes.Clear();
            _blockFlashes.Clear();
            _generator = new TerrainGenerator(unchecked((int)GD.Randi()));
            _chainVisualState = default;
            _markerPulseTime = 0f;
            _movePreviewInfo = null;
            _movePreviewVisible = false;
            _moveDebugInfo = null;
            _moveDebugVisible = false;
            UpdateCamera(PlayerStats.StartGridPosition);
            MarkExplored(PlayerStats.StartGridPosition, 9);
            QueueRedraw();
        }

        public bool IsInBounds(int col, int absoluteRow)
        {
            return absoluteRow >= PlayerStats.SurfaceRow;
        }

        public byte GetCell(int col, int absoluteRow)
        {
            if (!IsInBounds(col, absoluteRow))
            {
                return (byte)CellType.Bedrock;
            }

            var key = Pack(col, absoluteRow);
            if (_cellOverrides.TryGetValue(key, out var overriddenValue))
            {
                return overriddenValue;
            }

            var chunkX = FloorDiv(col, ChunkData.Width);
            var chunkY = FloorDiv(absoluteRow, ChunkData.Height);
            var localCol = PositiveMod(col, ChunkData.Width);
            var localRow = PositiveMod(absoluteRow, ChunkData.Height);
            return GetChunk(chunkX, chunkY).GetCell(localCol, localRow);
        }

        public void SetCell(int col, int absoluteRow, byte value)
        {
            SetCellInternal(col, absoluteRow, value, emitDigFlash: true, markExplored: value == (byte)CellType.Empty);
        }

        public void SetTransientCell(int col, int absoluteRow, byte value)
        {
            SetCellInternal(col, absoluteRow, value, emitDigFlash: false, markExplored: value == (byte)CellType.Empty);
        }

        private void SetCellInternal(int col, int absoluteRow, byte value, bool emitDigFlash, bool markExplored)
        {
            if (!IsInBounds(col, absoluteRow))
            {
                return;
            }

            var previous = GetCell(col, absoluteRow);
            if (previous == value)
            {
                return;
            }

            var key = Pack(col, absoluteRow);
            _cellOverrides[key] = value;
            if (value == (byte)CellType.Empty && markExplored)
            {
                _exploredCells.Add(key);
                if (emitDigFlash && previous != (byte)CellType.Empty)
                {
                    _digFlashes[key] = new DigFlash((CellType)previous, FlashDuration);
                }
            }

            QueueRedraw();
        }

        public void MarkExplored(Vector2I center, int size)
        {
            var half = size / 2;
            for (var row = center.Y - half; row <= center.Y + half; row++)
            {
                for (var col = center.X - half; col <= center.X + half; col++)
                {
                    if (!IsInBounds(col, row))
                    {
                        continue;
                    }

                    _exploredCells.Add(Pack(col, row));
                }
            }
        }

        public void CopyExploredCells(List<Vector2I> buffer)
        {
            buffer.Clear();
            foreach (var packed in _exploredCells)
            {
                Unpack(packed, out var col, out var row);
                buffer.Add(new Vector2I(col, row));
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

        public void SetChainVisualization(ChainVisualState chainVisualState)
        {
            if (_chainVisualState == chainVisualState)
            {
                return;
            }

            _chainVisualState = chainVisualState;
            QueueRedraw();
        }

        public void UpdateCamera(Vector2I playerCell)
        {
            var newLeftCol = playerCell.X - (VisibleColumns / 2);
            var newTopRow = Mathf.Max(PlayerStats.SurfaceRow, playerCell.Y - (VisibleRows / 2));
            var changed = newLeftCol != _cameraLeftCol || newTopRow != _cameraTopRow;

            _cameraLeftCol = newLeftCol;
            _cameraTopRow = newTopRow;
            changed |= EnsureChunksForWindow();

            if (changed)
            {
                QueueRedraw();
            }
        }

        public Vector2 GridToScreen(int col, int row)
        {
            return new Vector2((col - _cameraLeftCol) * CellSize, (row - _cameraTopRow) * CellSize);
        }

        public Vector2 GridToWorld(int col, int row)
        {
            return new Vector2(WorldOriginX + (col * CellSize), WorldOriginY + (row * CellSize));
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

            var backgroundTopLeft = GridToWorld(_cameraLeftCol, _cameraTopRow);
            DrawRect(new Rect2(backgroundTopLeft, new Vector2(VisibleColumns * CellSize, VisibleRows * CellSize)), new Color(0.05f, 0.07f, 0.10f));
            DrawSurfaceBand();

            var endCol = _cameraLeftCol + VisibleColumns;
            var endRow = _cameraTopRow + VisibleRows;

            for (var row = _cameraTopRow; row < endRow; row++)
            {
                var depthTier = TerrainGenerator.GetDepthTier(row);
                var runType = CellType.Empty;
                var runStart = _cameraLeftCol;

                for (var col = _cameraLeftCol; col <= endCol; col++)
                {
                    var currentType = col < endCol ? (CellType)GetCell(col, row) : CellType.Empty;
                    if (col == _cameraLeftCol)
                    {
                        runType = currentType;
                        runStart = _cameraLeftCol;
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
            DrawChainCheckpoint();
            DrawTerrainAccents();
            DrawMovePreview();
            DrawDigFlashes();
            DrawBlockFlashes();
            DrawMoveDebugOverlay();
        }

        private ChunkData GetChunk(int chunkX, int chunkY)
        {
            var chunkCoordinates = new Vector2I(chunkX, chunkY);
            if (_chunks.TryGetValue(chunkCoordinates, out var chunk))
            {
                return chunk;
            }

            chunk = new ChunkData(chunkX, chunkY);
            _generator.FillChunk(chunk);
            _chunks[chunkCoordinates] = chunk;
            return chunk;
        }

        private bool EnsureChunksForWindow()
        {
            var minCol = _cameraLeftCol - ChunkData.Width;
            var maxCol = _cameraLeftCol + VisibleColumns + ChunkData.Width;
            var minRow = Mathf.Max(PlayerStats.SurfaceRow, _cameraTopRow - ChunkData.Height);
            var maxRow = _cameraTopRow + VisibleRows + ChunkData.Height;

            var minChunkX = FloorDiv(minCol, ChunkData.Width);
            var maxChunkX = FloorDiv(maxCol, ChunkData.Width);
            var minChunkY = FloorDiv(minRow, ChunkData.Height);
            var maxChunkY = FloorDiv(maxRow, ChunkData.Height);
            var changed = false;

            for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                {
                    if (!_chunks.ContainsKey(new Vector2I(chunkX, chunkY)))
                    {
                        GetChunk(chunkX, chunkY);
                        changed = true;
                    }
                }
            }

            _chunkScratch.Clear();
            foreach (var coordinates in _chunks.Keys)
            {
                if (coordinates.X < minChunkX - 1 || coordinates.X > maxChunkX + 1 || coordinates.Y < minChunkY - 1 || coordinates.Y > maxChunkY + 1)
                {
                    _chunkScratch.Add(coordinates);
                }
            }

            for (var index = 0; index < _chunkScratch.Count; index++)
            {
                _chunks.Remove(_chunkScratch[index]);
                changed = true;
            }

            return changed;
        }

        private void DrawSurfaceBand()
        {
            var topLeft = GridToWorld(_cameraLeftCol, PlayerStats.SurfaceRow);
            var bandHeight = CellSize * 3f;
            DrawRect(new Rect2(new Vector2(topLeft.X, topLeft.Y - bandHeight), new Vector2(VisibleColumns * CellSize, bandHeight)), new Color(0.10f, 0.13f, 0.17f, 0.72f));
            DrawLine(new Vector2(topLeft.X, topLeft.Y), new Vector2(topLeft.X + (VisibleColumns * CellSize), topLeft.Y), new Color(0.94f, 0.92f, 0.72f, 0.88f), 2f);
        }

        private void DrawSpecialMarkers()
        {
            var endCol = _cameraLeftCol + VisibleColumns;
            var endRow = _cameraTopRow + VisibleRows;
            for (var row = _cameraTopRow; row < endRow; row++)
            {
                for (var col = _cameraLeftCol; col < endCol; col++)
                {
                    var type = (CellType)GetCell(col, row);
                    if (type != CellType.RecoveryPoint && type != CellType.Ore && type != CellType.Enemy)
                    {
                        continue;
                    }

                    var rect = new Rect2(GridToWorld(col, row), new Vector2(CellSize, CellSize));
                    if (type == CellType.RecoveryPoint)
                    {
                        DrawCircle(rect.GetCenter(), CellSize * 0.30f, new Color(0.90f, 1f, 0.98f, 0.92f));
                        DrawArc(rect.GetCenter(), CellSize * 0.38f, 0f, Mathf.Tau, 24, new Color(0.14f, 0.54f, 0.56f, 0.96f), 2f);
                    }
                    else if (type == CellType.Enemy)
                    {
                        DrawArc(rect.GetCenter(), CellSize * 0.26f, 0f, Mathf.Tau, 18, new Color(1f, 0.74f, 0.66f, 0.96f), 1.4f);
                        DrawLine(rect.Position + new Vector2(4f, 4f), rect.End - new Vector2(4f, 4f), new Color(1f, 0.92f, 0.88f, 0.70f), 1.2f);
                        DrawLine(rect.Position + new Vector2(CellSize - 4f, 4f), rect.Position + new Vector2(4f, CellSize - 4f), new Color(1f, 0.62f, 0.58f, 0.78f), 1.2f);
                    }
                    else
                    {
                        DrawLine(rect.Position + new Vector2(3f, CellSize * 0.5f), rect.End - new Vector2(3f, CellSize * 0.5f), new Color(1f, 0.96f, 0.70f, 0.78f), 1.6f);
                        DrawLine(rect.Position + new Vector2(CellSize * 0.5f, 3f), rect.Position + new Vector2(CellSize * 0.5f, CellSize - 3f), new Color(1f, 0.96f, 0.70f, 0.64f), 1.4f);
                    }
                }
            }
        }

        private void DrawTerrainAccents()
        {
            var endCol = _cameraLeftCol + VisibleColumns;
            var endRow = _cameraTopRow + VisibleRows;
            for (var row = _cameraTopRow; row < endRow; row++)
            {
                for (var col = _cameraLeftCol; col < endCol; col++)
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

        private void DrawChainCheckpoint()
        {
            if (!_chainVisualState.HasCheckpoint || !IsVisibleCell(_chainVisualState.CheckpointCell.X, _chainVisualState.CheckpointCell.Y))
            {
                return;
            }

            var center = GridToWorld(_chainVisualState.CheckpointCell.X, _chainVisualState.CheckpointCell.Y) + new Vector2(CellSize * 0.5f, CellSize * 0.5f);
            var pulse = 0.5f + (0.5f * Mathf.Sin(_markerPulseTime * 6f));
            var radius = (CellSize * 0.54f) + (pulse * 4f);
            var urgency = _chainVisualState.TimeRatio < 0.35f ? 1f : 0f;
            var outline = urgency > 0f
                ? new Color(1f, 0.56f, 0.40f, 0.96f)
                : new Color(1f, 0.86f, 0.36f, 0.96f);
            var fill = urgency > 0f
                ? new Color(1f, 0.46f, 0.28f, 0.18f)
                : new Color(1f, 0.82f, 0.24f, 0.16f);

            DrawCircle(center, radius * 0.92f, fill);
            DrawArc(center, radius, 0f, Mathf.Tau, 36, outline, 2.2f);
            DrawArc(center, radius + 4f + (pulse * 2f), 0f, Mathf.Tau * Mathf.Clamp(_chainVisualState.TimeRatio, 0.12f, 1f), 28, new Color(outline, 0.62f), 1.8f);
            DrawCircle(center, 2.4f + (pulse * 1.2f), new Color(1f, 0.98f, 0.84f, 0.96f));
        }

        private void DrawCellAccent(int col, int row, CellType type)
        {
            var rect = new Rect2(GridToWorld(col, row), new Vector2(CellSize, CellSize));
            switch (type)
            {
                case CellType.Dirt:
                    DrawLine(rect.Position + new Vector2(2f, 4f), rect.Position + new Vector2(CellSize - 2f, 4f), new Color(1f, 0.92f, 0.72f, 0.20f), 1.1f);
                    DrawLine(rect.Position + new Vector2(3f, CellSize - 4f), rect.Position + new Vector2(CellSize - 3f, CellSize - 5f), new Color(0.32f, 0.20f, 0.10f, 0.28f), 1f);
                    break;
                case CellType.Stone:
                    DrawLine(rect.Position + new Vector2(2f, 5f), rect.Position + new Vector2(CellSize - 2f, 5f), new Color(0.92f, 0.98f, 1f, 0.24f), 1.1f);
                    DrawLine(rect.Position + new Vector2(4f, 10f), rect.Position + new Vector2(CellSize - 4f, 10f), new Color(0.06f, 0.10f, 0.16f, 0.32f), 1f);
                    break;
                case CellType.HardRock:
                    DrawRect(rect.Grow(-1.4f), new Color(0.80f, 0.92f, 1f, 0.34f), false, 1.2f);
                    DrawLine(rect.Position + new Vector2(3f, 3f), rect.Position + new Vector2(CellSize - 3f, CellSize - 3f), new Color(0.94f, 0.98f, 1f, 0.18f), 1f);
                    DrawLine(rect.Position + new Vector2(CellSize - 3f, 3f), rect.Position + new Vector2(3f, CellSize - 3f), new Color(0.02f, 0.04f, 0.07f, 0.28f), 1f);
                    break;
                case CellType.Bedrock:
                    DrawRect(rect, new Color(0.92f, 0.74f, 0.96f, 0.24f), false, 1.8f);
                    DrawRect(rect.Grow(-4f), new Color(1f, 0.94f, 1f, 0.16f), false, 1f);
                    break;
                case CellType.Ore:
                    DrawArc(rect.GetCenter(), CellSize * 0.22f, 0f, Mathf.Tau, 18, new Color(1f, 0.98f, 0.76f, 0.30f), 1.2f);
                    break;
                case CellType.RecoveryPoint:
                    DrawRect(rect.Grow(-1f), new Color(0.82f, 1f, 0.94f, 0.24f), false, 1.1f);
                    break;
                case CellType.Item:
                    DrawLine(rect.Position + new Vector2(3f, 3f), rect.End - new Vector2(3f, 3f), new Color(1f, 0.84f, 0.56f, 0.26f), 1.1f);
                    DrawLine(rect.Position + new Vector2(CellSize - 3f, 3f), rect.Position + new Vector2(3f, CellSize - 3f), new Color(1f, 0.84f, 0.56f, 0.22f), 1.1f);
                    break;
                case CellType.Enemy:
                    DrawRect(rect.Grow(-1.8f), new Color(1f, 0.58f, 0.50f, 0.22f), false, 1.2f);
                    DrawCircle(rect.GetCenter(), CellSize * 0.12f, new Color(1f, 0.88f, 0.80f, 0.44f));
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
                if (!IsVisibleCell(col, row))
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
                if (!IsVisibleCell(col, row))
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
            if (!IsVisibleCell(cell.X, cell.Y) || !IsInBounds(cell.X, cell.Y))
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

        private bool IsVisibleCell(int col, int row)
        {
            return row >= _cameraTopRow && row < _cameraTopRow + VisibleRows && col >= _cameraLeftCol && col < _cameraLeftCol + VisibleColumns;
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

        private static int FloorDiv(int value, int divisor)
        {
            return value >= 0 ? value / divisor : -(((-value) + divisor - 1) / divisor);
        }

        private static int PositiveMod(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
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