using System.Collections.Generic;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class MiniMapOverlay : Control
    {
        private const float RefreshInterval = 0.12f;

        private readonly List<Vector2I> _exploredCells = new();
        private readonly List<Vector2I> _recoveryPoints = new();

        private ChunkManager _chunks;
        private PlayerStats _stats;
        private RecoveryPointManager _recoveryPointManager;
        private ChainManager _chainManager;
        private SonarReading _sonarReading = new(SonarSignalStrength.None, SonarTargetKind.None, CellType.Empty, Vector2I.Zero, 0, Vector2I.Zero);
        private float _backgroundOpacity = 0.78f;
        private float _refreshTimer;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetProcess(true);
        }

        public void Configure(float backgroundOpacity)
        {
            _backgroundOpacity = Mathf.Clamp(backgroundOpacity, 0.20f, 0.95f);
            QueueRedraw();
        }

        public void Initialize(ChunkManager chunks, PlayerStats stats, RecoveryPointManager recoveryPointManager, ChainManager chainManager)
        {
            _chunks = chunks;
            _stats = stats;
            _recoveryPointManager = recoveryPointManager;
            _chainManager = chainManager;
            _refreshTimer = 0f;
            QueueRedraw();
        }

        public void SetSonarTarget(SonarReading reading)
        {
            _sonarReading = reading;
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            if (_chunks == null || _stats == null || !Visible)
            {
                return;
            }

            _refreshTimer -= (float)delta;
            if (_refreshTimer > 0f)
            {
                return;
            }

            _refreshTimer = RefreshInterval;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_chunks == null || _stats == null)
            {
                return;
            }

            _chunks.CopyExploredCells(_exploredCells);
            _recoveryPoints.Clear();
            if (_recoveryPointManager != null)
            {
                _recoveryPoints.AddRange(_recoveryPointManager.Points);
            }

            var bounds = ResolveBounds();
            var innerRect = new Rect2(new Vector2(6f, 6f), Size - new Vector2(12f, 12f));
            DrawRect(innerRect, new Color(0.03f, 0.05f, 0.08f, _backgroundOpacity));
            DrawRect(innerRect, new Color(0.72f, 0.84f, 0.94f, 0.28f), false, 1.2f);

            var scale = Mathf.Max(1f, Mathf.Min(innerRect.Size.X / bounds.Size.X, innerRect.Size.Y / bounds.Size.Y));
            var contentSize = new Vector2(bounds.Size.X * scale, bounds.Size.Y * scale);
            var origin = innerRect.Position + ((innerRect.Size - contentSize) * 0.5f);
            DrawSurfaceLine(bounds, origin, scale);

            for (var index = 0; index < _exploredCells.Count; index++)
            {
                DrawCell(origin, bounds, _exploredCells[index], scale, new Color(0.78f, 0.90f, 0.96f, 0.82f));
            }

            for (var index = 0; index < _recoveryPoints.Count; index++)
            {
                DrawCell(origin, bounds, _recoveryPoints[index], scale, new Color(0.42f, 1f, 0.90f, 0.96f));
            }

            if (_chainManager != null && _chainManager.HasActiveCheckpoint)
            {
                DrawPulseCell(origin, bounds, _chainManager.ActiveCheckpoint, scale, new Color(0.98f, 0.78f, 0.30f, 0.94f), true);
            }

            if (_sonarReading.TargetKind != SonarTargetKind.None)
            {
                var sonarColor = _sonarReading.TargetKind == SonarTargetKind.Danger
                    ? new Color(1.00f, 0.54f, 0.46f, 0.88f)
                    : new Color(0.54f, 0.96f, 1.00f, 0.84f);
                DrawPulseCell(origin, bounds, _sonarReading.TargetCell, scale, sonarColor, false);
            }

            DrawCell(origin, bounds, PlayerStats.StartGridPosition, scale, new Color(1f, 0.84f, 0.38f, 1f));
            DrawCell(origin, bounds, _stats.GridPosition, scale, new Color(1f, 0.46f, 0.40f, 1f));
            DrawRect(new Rect2(origin, contentSize), new Color(1f, 1f, 1f, 0.04f), false, 0.8f);
        }

        private Rect2I ResolveBounds()
        {
            var minX = Mathf.Min(PlayerStats.StartGridPosition.X, _stats.GridPosition.X);
            var maxX = Mathf.Max(PlayerStats.StartGridPosition.X, _stats.GridPosition.X);
            var minY = Mathf.Min(PlayerStats.SurfaceRow - 2, Mathf.Min(PlayerStats.StartGridPosition.Y, _stats.GridPosition.Y));
            var maxY = Mathf.Max(PlayerStats.StartGridPosition.Y, _stats.GridPosition.Y);

            for (var index = 0; index < _exploredCells.Count; index++)
            {
                var cell = _exploredCells[index];
                minX = Mathf.Min(minX, cell.X);
                maxX = Mathf.Max(maxX, cell.X);
                minY = Mathf.Min(minY, cell.Y);
                maxY = Mathf.Max(maxY, cell.Y);
            }

            for (var index = 0; index < _recoveryPoints.Count; index++)
            {
                var cell = _recoveryPoints[index];
                minX = Mathf.Min(minX, cell.X);
                maxX = Mathf.Max(maxX, cell.X);
                minY = Mathf.Min(minY, cell.Y);
                maxY = Mathf.Max(maxY, cell.Y);
            }

            if (_chainManager != null && _chainManager.HasActiveCheckpoint)
            {
                minX = Mathf.Min(minX, _chainManager.ActiveCheckpoint.X);
                maxX = Mathf.Max(maxX, _chainManager.ActiveCheckpoint.X);
                minY = Mathf.Min(minY, _chainManager.ActiveCheckpoint.Y);
                maxY = Mathf.Max(maxY, _chainManager.ActiveCheckpoint.Y);
            }

            if (_sonarReading.TargetKind != SonarTargetKind.None)
            {
                minX = Mathf.Min(minX, _sonarReading.TargetCell.X);
                maxX = Mathf.Max(maxX, _sonarReading.TargetCell.X);
                minY = Mathf.Min(minY, _sonarReading.TargetCell.Y);
                maxY = Mathf.Max(maxY, _sonarReading.TargetCell.Y);
            }

            minX -= 4;
            maxX += 4;
            minY = Mathf.Min(PlayerStats.SurfaceRow - 2, minY - 2);
            maxY += 4;

            var width = Mathf.Max(24, maxX - minX + 1);
            var height = Mathf.Max(24, maxY - minY + 1);
            return new Rect2I(minX, minY, width, height);
        }

        private void DrawCell(Vector2 origin, Rect2I bounds, Vector2I cell, float scale, Color color)
        {
            if (cell.X < bounds.Position.X || cell.X >= bounds.Position.X + bounds.Size.X || cell.Y < bounds.Position.Y || cell.Y >= bounds.Position.Y + bounds.Size.Y)
            {
                return;
            }

            var rect = new Rect2(
                origin.X + ((cell.X - bounds.Position.X) * scale),
                origin.Y + ((cell.Y - bounds.Position.Y) * scale),
                Mathf.Max(1f, scale),
                Mathf.Max(1f, scale));
            DrawRect(rect, color);
        }

        private void DrawPulseCell(Vector2 origin, Rect2I bounds, Vector2I cell, float scale, Color color, bool drawRing)
        {
            if (cell.X < bounds.Position.X || cell.X >= bounds.Position.X + bounds.Size.X || cell.Y < bounds.Position.Y || cell.Y >= bounds.Position.Y + bounds.Size.Y)
            {
                return;
            }

            var rect = new Rect2(
                origin.X + ((cell.X - bounds.Position.X) * scale),
                origin.Y + ((cell.Y - bounds.Position.Y) * scale),
                Mathf.Max(1.4f, scale),
                Mathf.Max(1.4f, scale));
            DrawRect(rect, color);
            if (drawRing)
            {
                DrawRect(rect.Grow(1.8f), new Color(1f, 0.92f, 0.58f, 0.86f), false, 1.2f);
            }
        }

        private void DrawSurfaceLine(Rect2I bounds, Vector2 origin, float scale)
        {
            if (PlayerStats.SurfaceRow < bounds.Position.Y || PlayerStats.SurfaceRow >= bounds.Position.Y + bounds.Size.Y)
            {
                return;
            }

            var y = origin.Y + ((PlayerStats.SurfaceRow - bounds.Position.Y) * scale);
            DrawLine(new Vector2(origin.X, y), new Vector2(origin.X + (bounds.Size.X * scale), y), new Color(0.98f, 0.92f, 0.62f, 0.84f), 1.2f);
        }
    }
}