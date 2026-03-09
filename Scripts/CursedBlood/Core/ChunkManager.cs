using System.Collections.Generic;
using Godot;

namespace CursedBlood.Core
{
    public partial class ChunkManager : Node2D
    {
        private const float FlashDuration = 0.14f;

        private readonly Dictionary<int, ChunkData> _chunks = new();
        private readonly Dictionary<long, DigFlash> _digFlashes = new();
        private readonly List<int> _chunkScratch = new();
        private readonly List<long> _flashScratch = new();

        private TerrainGenerator _generator;
        private int _cameraTopRow;

        public const int CellSize = 16;
        public const int ChunkHeight = 16;
        public const int Columns = 67;
        public const int VisibleRows = 87;
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
            if (_digFlashes.Count == 0)
            {
                return;
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
                flash.TimeLeft -= (float)delta;
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

            if (needsRefresh)
            {
                QueueRedraw();
            }
        }

        public void Initialize()
        {
            _chunks.Clear();
            _digFlashes.Clear();
            _cameraTopRow = 0;
            _generator = new TerrainGenerator(unchecked((int)GD.Randi()));

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

        public byte GetCell(int col, int absoluteRow)
        {
            if (col < 0 || col >= Columns || absoluteRow < 0)
            {
                return (byte)CellType.Bedrock;
            }

            var chunkIndex = absoluteRow / ChunkHeight;
            var localRow = absoluteRow % ChunkHeight;
            return GetChunk(chunkIndex).GetCell(col, localRow);
        }

        public void SetCell(int col, int absoluteRow, byte value)
        {
            if (col < 0 || col >= Columns || absoluteRow < 0)
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

        private void DrawRun(int row, int startCol, int length, CellType type, int depthTier)
        {
            var rect = new Rect2(GridToWorld(startCol, row), new Vector2(length * CellSize, CellSize));
            DrawRect(rect, CellTypeUtil.GetColor(type, depthTier));
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
    }
}