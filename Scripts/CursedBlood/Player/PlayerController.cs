using System;
using System.Collections.Generic;
using CursedBlood.Core;
using CursedBlood.UI;
using Godot;

namespace CursedBlood.Player
{
    public partial class PlayerController : Node2D
    {
        private const float GuardHoldSeconds = 0.18f;

        private enum DirectionHintKind
        {
            Open,
            Diggable,
            Blocked
        }

        private readonly record struct DirectionHintState(Vector2I Direction, DirectionHintKind Kind, float Hardness, MoveBlockReason BlockReason, CellType BlockedType);

        private static readonly Vector2I[] HintDirections =
        {
            Vector2I.Up,
            new Vector2I(1, -1),
            Vector2I.Right,
            new Vector2I(1, 1),
            Vector2I.Down,
            new Vector2I(-1, 1),
            Vector2I.Left,
            new Vector2I(-1, -1)
        };

        private readonly List<Vector2I> _digBuffer = new(24);
        private readonly List<Vector2I> _occupancyBuffer = new(25);
        private readonly List<Vector2I> _hintDigBuffer = new(24);
        private readonly List<Vector2I> _hintOccupancyBuffer = new(25);
        private readonly MoveDebugInfo _moveDebugInfo = new();
        private readonly DirectionHintState[] _directionHints = new DirectionHintState[HintDirections.Length];
        private PlayerVisualController _visualController;
        private Vector2I _moveDirection = Vector2I.Down;
        private Vector2I _bufferedDirection = Vector2I.Zero;
        private Vector2I _moveTargetGrid;
        private Vector2 _moveStartPosition;
        private Vector2 _moveEndPosition;
        private Vector2 _pointerStart;
        private bool _pointerActive;
        private bool _pointerMoved;
        private bool _touchGuarding;
        private bool _isMoving;
        private bool _wasGuarding;
        private float _pointerHoldTime;
        private float _moveTimer;
        private float _currentMoveDuration;
        private float _sonarPulseTimer;
        private int _lastPlayerSize;
        private int _lastPreviewHash;
        private SonarReading _sonarReading = new(SonarSignalStrength.None, CellType.Empty, Vector2I.Zero, 0);
        private bool _directionHintsVisible = true;
        private bool _sonarVisualsVisible = true;

        [Export]
        public float SwipeThreshold { get; set; } = 32f;

        [Export]
        public float DirectionHintRadius { get; set; } = 28f;

        [Export]
        public float DirectionHintSize { get; set; } = 9f;

        [Export]
        public float SonarPulseSpeed { get; set; } = 3.8f;

        [Export]
        public float PlayerGlowRadiusPadding { get; set; } = 12f;

        public ChunkManager Chunks { get; set; }

        public PlayerStats Stats { get; set; }

        public VirtualPad VirtualPad { get; set; }

        public bool InputEnabled { get; set; } = true;

        public bool IsGuarding => Input.IsKeyPressed(Key.Space) || _touchGuarding;

        public Vector2I CurrentDirection => _moveDirection;

        public MoveDebugInfo MoveDebugInfo => _moveDebugInfo;

        public string MovementStatusText => BuildMovementStatusText();

        public override void _Ready()
        {
            EnsureVisualController();
            EnsureVirtualPad();
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            if (!InputEnabled || Stats == null || Chunks == null || !Stats.IsAlive)
            {
                return;
            }

            HandleKeyboardInput();
            UpdateTouchGuard((float)delta);
            ProcessMovement((float)delta);
            RefreshMovePreview();
            RefreshDirectionHints();
            _sonarPulseTimer += (float)delta;

            if (_lastPlayerSize != Stats.PlayerSize)
            {
                _lastPlayerSize = Stats.PlayerSize;
                QueueRedraw();
            }

            var guarding = IsGuarding;
            if (_wasGuarding != guarding)
            {
                _wasGuarding = guarding;
                QueueRedraw();
            }

            _visualController?.UpdateVisual(_moveDirection, _isMoving, Stats.PlayerSize);

            if (_directionHintsVisible || _sonarVisualsVisible || IsGuarding || _isMoving)
            {
                QueueRedraw();
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (!InputEnabled || Stats == null || !Stats.IsAlive)
            {
                return;
            }

            switch (@event)
            {
                case InputEventScreenTouch screenTouch:
                    HandlePointerPressed(screenTouch.Pressed, screenTouch.Position);
                    break;
                case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                    HandlePointerPressed(mouseButton.Pressed, mouseButton.Position);
                    break;
                case InputEventScreenDrag screenDrag:
                    DetectSwipe(screenDrag.Position);
                    break;
                case InputEventMouseMotion mouseMotion when _pointerActive:
                    DetectSwipe(mouseMotion.Position);
                    break;
            }
        }

        public override void _Draw()
        {
            if (Stats == null)
            {
                return;
            }

            var sizePixels = Stats.PlayerSize * ChunkManager.CellSize;
            var half = sizePixels * 0.5f;
            var pulse = 0.5f + (0.5f * Mathf.Sin(_sonarPulseTimer * 4f));
            DrawCircle(new Vector2(0f, half * 0.18f), half * 0.78f, new Color(0f, 0f, 0f, 0.18f));
            DrawCircle(Vector2.Zero, half + PlayerGlowRadiusPadding + (pulse * 1.8f), new Color(0.94f, 0.98f, 1f, _isMoving ? 0.12f : 0.08f));
            DrawArc(Vector2.Zero, half + PlayerGlowRadiusPadding, 0f, Mathf.Tau, 48, new Color(0.95f, 0.99f, 1f, 0.82f), 2.2f);

            var facing = new Vector2(_moveDirection.X, _moveDirection.Y);
            if (facing != Vector2.Zero)
            {
                DrawCircle(facing.Normalized() * (half + 8f), 3.4f, new Color(1f, 1f, 1f, 0.92f));
            }

            if (IsGuarding)
            {
                DrawArc(Vector2.Zero, half + 10f, 0f, Mathf.Tau, 48, new Color(0.75f, 0.92f, 1f), 4f);
            }

            DrawDirectionHints();
            DrawSonarPulse();
        }

        public void Reset()
        {
            EnsureVisualController();
            EnsureVirtualPad();
            _moveDirection = Vector2I.Down;
            _bufferedDirection = Vector2I.Zero;
            _moveTargetGrid = Vector2I.Zero;
            _moveTimer = 0f;
            _currentMoveDuration = 0f;
            _pointerActive = false;
            _pointerMoved = false;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            _isMoving = false;
            _wasGuarding = false;
            _lastPlayerSize = Stats?.PlayerSize ?? 3;
            _moveDebugInfo.Reset(Stats?.GridPosition ?? Vector2I.Zero, _moveDirection);
            VirtualPad?.End();
            SyncToStatsPosition();
            RefreshMovePreview();
            RefreshDirectionHints();
            _visualController?.UpdateVisual(_moveDirection, false, Stats?.PlayerSize ?? 5);
            QueueRedraw();
        }

        public void SyncToStatsPosition()
        {
            if (Stats == null || Chunks == null)
            {
                return;
            }

            Position = Chunks.GridToWorldCenter(Stats.GridPosition.X, Stats.GridPosition.Y);
            _moveStartPosition = Position;
            _moveEndPosition = Position;
            _isMoving = false;
            _moveTimer = 0f;
            Chunks?.RequestRefresh();
        }

        public Vector2 GetCurrentWorldPosition()
        {
            return GlobalPosition;
        }

        public void CancelTouchInput()
        {
            _pointerActive = false;
            _pointerMoved = false;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            VirtualPad?.End();
        }

        public void SetDirectionHintsVisible(bool visible)
        {
            _directionHintsVisible = visible;
            QueueRedraw();
        }

        public void SetSonarVisualsVisible(bool visible)
        {
            _sonarVisualsVisible = visible;
            QueueRedraw();
        }

        public void SetSonarReading(SonarReading reading)
        {
            _sonarReading = reading;
        }

        public string GetDebugSummary()
        {
            var directionName = GetDirectionName(_moveDirection);
            var lines = new List<string>(8)
            {
                $"Direction: {directionName}",
                $"Visual: {_visualController?.CurrentDirectionName ?? directionName}",
                $"Status: {MovementStatusText}",
                $"Moving: {_isMoving}",
                $"Guard: {IsGuarding}",
                $"Preview: {_moveDebugInfo.CanMove} / Dig {_moveDebugInfo.RequiresDig}"
            };

            if (_moveDebugInfo.HasTarget)
            {
                lines.Add($"Target: {_moveDebugInfo.Target}");
                lines.Add($"CanMove: {_moveDebugInfo.CanMove}");
                lines.Add($"Cell: {CellTypeUtil.GetName(_moveDebugInfo.TargetCellType)}");
                lines.Add($"Hardness: {_moveDebugInfo.MaxHardness:0.00}");
                lines.Add($"Block: {_moveDebugInfo.BlockReason}");
            }

            return string.Join("\n", lines);
        }

        private void EnsureVisualController()
        {
            _visualController ??= GetNodeOrNull<PlayerVisualController>("PlayerVisual");
            if (_visualController != null)
            {
                return;
            }

            _visualController = new PlayerVisualController
            {
                Name = "PlayerVisual"
            };
            AddChild(_visualController);
        }

        private void EnsureVirtualPad()
        {
            VirtualPad ??= GetNodeOrNull<VirtualPad>("VirtualPad");
            if (VirtualPad != null)
            {
                return;
            }

            VirtualPad = new VirtualPad
            {
                Name = "VirtualPad",
                TopLevel = true
            };
            AddChild(VirtualPad);
        }

        private void HandleKeyboardInput()
        {
            var inputDirection = Vector2I.Zero;
            if (Input.IsKeyPressed(Key.Left))
            {
                inputDirection.X -= 1;
            }

            if (Input.IsKeyPressed(Key.Right))
            {
                inputDirection.X += 1;
            }

            if (Input.IsKeyPressed(Key.Up))
            {
                inputDirection.Y -= 1;
            }

            if (Input.IsKeyPressed(Key.Down))
            {
                inputDirection.Y += 1;
            }

            if (inputDirection == Vector2I.Zero && VirtualPad?.IsActive == true)
            {
                inputDirection = VirtualPad.GetSnappedDirection();
            }

            if (inputDirection != Vector2I.Zero)
            {
                RequestDirection(inputDirection);
            }
        }

        private void UpdateTouchGuard(float delta)
        {
            if (!_pointerActive || _pointerMoved)
            {
                return;
            }

            _pointerHoldTime += delta;
            if (_pointerHoldTime >= GuardHoldSeconds)
            {
                _touchGuarding = true;
            }
        }

        private void ProcessMovement(float delta)
        {
            if (_isMoving)
            {
                _moveTimer += delta;
                var progress = _currentMoveDuration <= 0f ? 1f : Mathf.Clamp(_moveTimer / _currentMoveDuration, 0f, 1f);
                Position = _moveStartPosition.Lerp(_moveEndPosition, progress);

                if (progress >= 1f)
                {
                    CompleteMove();
                }

                return;
            }

            if (IsGuarding)
            {
                return;
            }

            if (_bufferedDirection != Vector2I.Zero)
            {
                _moveDirection = _bufferedDirection;
                _bufferedDirection = Vector2I.Zero;
                QueueRedraw();
            }

            TryStartMove();
        }

        private void RequestDirection(Vector2I direction)
        {
            if (direction == Vector2I.Zero)
            {
                return;
            }

            if (_isMoving)
            {
                _bufferedDirection = direction;
                return;
            }

            if (_moveDirection != direction)
            {
                _moveDirection = direction;
                Chunks?.RequestRefresh();
                QueueRedraw();
            }
        }

        private bool TryStartMove()
        {
            if (!EvaluateMove(Stats.GridPosition, _moveDirection, _moveDebugInfo, _digBuffer, _occupancyBuffer, out _, out var maxHardness))
            {
                if (_moveDebugInfo.HasBlockedCell)
                {
                    Chunks?.FlashBlockedCell(_moveDebugInfo.BlockedCell, _moveDebugInfo.BlockReason);
                }

                return false;
            }

            var dugCount = DigHelper.ExecuteDig(Chunks, _digBuffer);
            if (dugCount > 0)
            {
                Stats.RegisterDig(dugCount);
            }

            var target = Stats.GridPosition + _moveDirection;
            _moveTargetGrid = target;
            _moveStartPosition = Chunks.GridToWorldCenter(Stats.GridPosition.X, Stats.GridPosition.Y);
            _moveEndPosition = Chunks.GridToWorldCenter(target.X, target.Y);
            _moveTimer = 0f;
            _currentMoveDuration = Stats.EffectiveMoveInterval * maxHardness;
            _isMoving = true;
            _moveDebugInfo.AllowMove();
            return true;
        }

        private void CompleteMove()
        {
            _isMoving = false;
            Stats.GridPosition = _moveTargetGrid;
            Stats.RegisterDepth(_moveTargetGrid.Y);
            Position = _moveEndPosition;
            Chunks.UpdateCamera(Stats.GridPosition.Y);
            Chunks.RequestRefresh();
        }

        private void RefreshMovePreview()
        {
            if (Stats == null || Chunks == null)
            {
                return;
            }

            var previewOrigin = _isMoving ? _moveTargetGrid : Stats.GridPosition;
            var previewDirection = _isMoving && _bufferedDirection != Vector2I.Zero ? _bufferedDirection : _moveDirection;
            EvaluateMove(previewOrigin, previewDirection, _moveDebugInfo, _digBuffer, _occupancyBuffer, out _, out _);
            var previewHash = ComputeMoveDebugHash(_moveDebugInfo);
            if (previewHash != _lastPreviewHash)
            {
                _lastPreviewHash = previewHash;
                Chunks.RequestRefresh();
            }
        }

        private void RefreshDirectionHints()
        {
            if (Stats == null || Chunks == null)
            {
                return;
            }

            var origin = _isMoving ? _moveTargetGrid : Stats.GridPosition;
            for (var index = 0; index < HintDirections.Length; index++)
            {
                _directionHints[index] = EvaluateDirectionHint(origin, HintDirections[index]);
            }
        }

        private DirectionHintState EvaluateDirectionHint(Vector2I origin, Vector2I direction)
        {
            var canMove = EvaluateMove(origin, direction, null, _hintDigBuffer, _hintOccupancyBuffer, out var requiresDig, out var maxHardness);
            if (!canMove)
            {
                var blockedType = _hintDigBuffer.Count > 0 ? GetCellType(_hintDigBuffer[0]) : CellType.Bedrock;
                return new DirectionHintState(direction, DirectionHintKind.Blocked, maxHardness, MoveBlockReason.Bedrock, blockedType);
            }

            return new DirectionHintState(direction, requiresDig ? DirectionHintKind.Diggable : DirectionHintKind.Open, maxHardness, MoveBlockReason.None, CellType.Empty);
        }

        private bool EvaluateMove(Vector2I origin, Vector2I direction, MoveDebugInfo info, List<Vector2I> digAreaBuffer, List<Vector2I> occupancyAreaBuffer, out bool requiresDig, out float maxHardness)
        {
            requiresDig = false;
            maxHardness = 1f;
            info?.Reset(origin, direction);

            if (direction == Vector2I.Zero || Stats == null || Chunks == null)
            {
                info?.SetRequiresDig(false);
                return false;
            }

            DigHelper.FillDigArea(digAreaBuffer, origin, direction, Stats.DigWidth, Stats.DigShape, Stats.PlayerSize);
            info?.SetDigArea(digAreaBuffer);
            if (digAreaBuffer.Count == 0)
            {
                info?.SetRequiresDig(false);
                return false;
            }

            var target = origin + direction;
            BuildOccupancyArea(occupancyAreaBuffer, target, Stats.PlayerSize);
            info?.SetOccupancyArea(occupancyAreaBuffer);
            info?.SetTargetCellType(GetCellType(target));

            for (var index = 0; index < digAreaBuffer.Count; index++)
            {
                var cell = digAreaBuffer[index];
                if (!Chunks.IsInBounds(cell.X, cell.Y))
                {
                    info?.Block(MoveBlockReason.OutOfBounds, cell, CellType.Bedrock);
                    info?.SetRequiresDig(requiresDig);
                    return false;
                }

                var type = (CellType)Chunks.GetCell(cell.X, cell.Y);
                if (CellTypeUtil.RequiresDig(type))
                {
                    requiresDig = true;
                }

                if (!CellTypeUtil.IsDiggable(type))
                {
                    info?.Block(type == CellType.Bedrock ? MoveBlockReason.Bedrock : MoveBlockReason.Occupancy, cell, type);
                    info?.SetRequiresDig(requiresDig);
                    return false;
                }

                maxHardness = Mathf.Max(maxHardness, CellTypeUtil.GetHardness(type));
                info?.ConsiderHardness(type);
            }

            if (!CanOccupyAfterDig(digAreaBuffer, occupancyAreaBuffer, info))
            {
                info?.SetRequiresDig(requiresDig);
                return false;
            }

            info?.SetRequiresDig(requiresDig);
            info?.AllowMove();
            return true;
        }

        private bool CanOccupyAfterDig(IReadOnlyList<Vector2I> digArea, IReadOnlyList<Vector2I> occupancyArea, MoveDebugInfo info)
        {
            for (var index = 0; index < occupancyArea.Count; index++)
            {
                var cell = occupancyArea[index];
                if (ContainsCell(digArea, cell.X, cell.Y))
                {
                    continue;
                }

                if (!Chunks.IsInBounds(cell.X, cell.Y))
                {
                    info?.Block(MoveBlockReason.OutOfBounds, cell, CellType.Bedrock);
                    return false;
                }

                var type = (CellType)Chunks.GetCell(cell.X, cell.Y);
                if (!CellTypeUtil.IsPassable(type))
                {
                    info?.Block(MoveBlockReason.Occupancy, cell, type);
                    return false;
                }
            }

            return true;
        }

        private static void BuildOccupancyArea(List<Vector2I> buffer, Vector2I center, int size)
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

        private CellType GetCellType(Vector2I cell)
        {
            if (Chunks == null || !Chunks.IsInBounds(cell.X, cell.Y))
            {
                return CellType.Bedrock;
            }

            return (CellType)Chunks.GetCell(cell.X, cell.Y);
        }

        private void DrawDirectionHints()
        {
            if (Stats == null)
            {
                return;
            }

            var playerRadius = Stats.PlayerSize * ChunkManager.CellSize * 0.5f;
            var pulse = 0.55f + (0.45f * Mathf.Sin(_sonarPulseTimer * SonarPulseSpeed));

            for (var index = 0; index < _directionHints.Length; index++)
            {
                var state = _directionHints[index];
                var directionVector = new Vector2(state.Direction.X, state.Direction.Y).Normalized();
                if (directionVector == Vector2.Zero)
                {
                    continue;
                }

                var highlightedBySonar = _sonarVisualsVisible && _sonarReading.Strength != SonarSignalStrength.None && _sonarReading.Direction == state.Direction;
                if (!_directionHintsVisible && !highlightedBySonar)
                {
                    continue;
                }

                var markerCenter = directionVector * (playerRadius + DirectionHintRadius);
                var tetherStart = directionVector * (playerRadius + 6f);
                var emphasized = highlightedBySonar && _sonarReading.Strength != SonarSignalStrength.Far;

                switch (state.Kind)
                {
                    case DirectionHintKind.Open:
                    {
                        var fill = highlightedBySonar
                            ? new Color(0.64f, 0.98f, 1f, 0.88f * pulse)
                            : new Color(0.98f, 0.99f, 1f, 0.84f);
                        var outline = highlightedBySonar
                            ? new Color(0.74f, 1f, 1f, 0.96f)
                            : new Color(0.88f, 0.92f, 0.98f, 0.92f);
                        DrawLine(tetherStart, markerCenter, outline, emphasized ? 2.4f : 1.7f);
                        DrawDirectionArrow(markerCenter, directionVector, DirectionHintSize + (emphasized ? 2f : 0f), fill, outline);
                        break;
                    }
                    case DirectionHintKind.Diggable:
                    {
                        var fill = highlightedBySonar
                            ? new Color(0.70f, 1f, 0.96f, 0.86f * pulse)
                            : new Color(0.98f, 0.78f, 0.24f, 0.88f);
                        var outline = highlightedBySonar
                            ? new Color(0.76f, 1f, 1f, 0.98f)
                            : new Color(1f, 0.90f, 0.62f, 0.96f);
                        DrawLine(tetherStart, markerCenter, outline, emphasized ? 2.4f : 1.8f);
                        DrawDirectionArrow(markerCenter, directionVector, DirectionHintSize + 0.5f + (emphasized ? 2f : 0f), fill, outline);
                        break;
                    }
                    case DirectionHintKind.Blocked when _directionHintsVisible:
                    {
                        var color = highlightedBySonar
                            ? new Color(0.72f, 1f, 1f, 0.80f * pulse)
                            : new Color(1f, 0.40f, 0.40f, 0.82f);
                        DrawLine(tetherStart, markerCenter, color, 1.3f);
                        DrawCircle(markerCenter, emphasized ? 6f : 4.6f, color);
                        break;
                    }
                }

                if (highlightedBySonar)
                {
                    DrawArc(markerCenter, DirectionHintSize + 5f + (pulse * 2f), 0f, Mathf.Tau, 28, new Color(0.68f, 1f, 0.98f, 0.46f), 1.8f);
                }
            }
        }

        private void DrawSonarPulse()
        {
            if (!_sonarVisualsVisible || Stats == null || _sonarReading.Strength == SonarSignalStrength.None)
            {
                return;
            }

            var baseRadius = (Stats.PlayerSize * ChunkManager.CellSize * 0.5f) + 18f;
            var pulse = 0.5f + (0.5f * Mathf.Sin(_sonarPulseTimer * SonarPulseSpeed));
            var pulseRadius = baseRadius + (_sonarReading.Strength switch
            {
                SonarSignalStrength.Near => 18f,
                SonarSignalStrength.Medium => 13f,
                _ => 8f
            } * pulse);
            var color = _sonarReading.Strength switch
            {
                SonarSignalStrength.Near => new Color(0.48f, 1f, 0.96f, 0.34f),
                SonarSignalStrength.Medium => new Color(0.70f, 0.94f, 1f, 0.26f),
                _ => new Color(0.96f, 0.88f, 0.66f, 0.18f)
            };
            DrawArc(Vector2.Zero, pulseRadius, 0f, Mathf.Tau, 56, color, 2.2f);
        }

        private void DrawDirectionArrow(Vector2 center, Vector2 directionVector, float size, Color fill, Color outline)
        {
            var perpendicular = new Vector2(-directionVector.Y, directionVector.X);
            var tip = center + (directionVector * size);
            var tailCenter = center - (directionVector * (size * 0.58f));
            var left = tailCenter + (perpendicular * (size * 0.62f));
            var right = tailCenter - (perpendicular * (size * 0.62f));
            DrawLine(tailCenter, tip, fill, 3f);
            DrawLine(tip, left, outline, 2.2f);
            DrawLine(tip, right, outline, 2.2f);
            DrawCircle(center, size * 0.34f, fill);
        }

        private static bool ContainsCell(IReadOnlyList<Vector2I> cells, int col, int row)
        {
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index].X == col && cells[index].Y == row)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComputeMoveDebugHash(MoveDebugInfo info)
        {
            var hash = new HashCode();
            hash.Add(info.Direction);
            hash.Add(info.Target);
            hash.Add(info.TargetCellType);
            hash.Add(info.MaxHardness);
            hash.Add(info.RequiresDig);
            hash.Add(info.CanMove);
            hash.Add(info.BlockReason);
            hash.Add(info.BlockedCell);
            for (var index = 0; index < info.DigArea.Count; index++)
            {
                hash.Add(info.DigArea[index]);
            }

            for (var index = 0; index < info.OccupancyArea.Count; index++)
            {
                hash.Add(info.OccupancyArea[index]);
            }

            return hash.ToHashCode();
        }

        private void HandlePointerPressed(bool pressed, Vector2 position)
        {
            _pointerActive = pressed;
            _pointerMoved = false;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            _pointerStart = position;

            if (pressed)
            {
                VirtualPad?.Begin(position);
            }
            else
            {
                VirtualPad?.End();
            }
        }

        private void DetectSwipe(Vector2 currentPosition)
        {
            VirtualPad?.UpdatePointer(currentPosition);
            var virtualPadDirection = VirtualPad?.GetSnappedDirection() ?? Vector2I.Zero;
            if (virtualPadDirection != Vector2I.Zero)
            {
                _pointerMoved = true;
                _touchGuarding = false;
                _pointerHoldTime = 0f;
                RequestDirection(virtualPadDirection);
                return;
            }

            var delta = currentPosition - _pointerStart;
            if (delta.Length() < SwipeThreshold)
            {
                return;
            }

            _pointerMoved = true;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            _pointerStart = currentPosition;
            RequestDirection(SnapToOctant(delta));
        }

        private string BuildMovementStatusText()
        {
            var directionName = GetDirectionName(_moveDirection);
            if (!InputEnabled || Stats == null || !Stats.IsAlive)
            {
                return $"停止 {directionName}";
            }

            if (IsGuarding)
            {
                return $"Guard {directionName}";
            }

            if (_isMoving)
            {
                return $"移動 {directionName}";
            }

            if (_bufferedDirection != Vector2I.Zero)
            {
                return $"入力待機 {GetDirectionName(_bufferedDirection)}";
            }

            return $"待機 {directionName}";
        }

        private static string GetDirectionName(Vector2I direction)
        {
            return direction switch
            {
                var d when d == Vector2I.Down => "down",
                var d when d == new Vector2I(-1, 1) => "down_left",
                var d when d == new Vector2I(1, 1) => "down_right",
                var d when d == Vector2I.Left => "left",
                var d when d == Vector2I.Right => "right",
                var d when d == Vector2I.Up => "up",
                var d when d == new Vector2I(-1, -1) => "up_left",
                var d when d == new Vector2I(1, -1) => "up_right",
                _ => "down"
            };
        }

        private static Vector2I SnapToOctant(Vector2 delta)
        {
            var octant = Mathf.PosMod(Mathf.RoundToInt(delta.Angle() / (Mathf.Pi / 4f)), 8);
            return octant switch
            {
                0 => Vector2I.Right,
                1 => new Vector2I(1, 1),
                2 => Vector2I.Down,
                3 => new Vector2I(-1, 1),
                4 => Vector2I.Left,
                5 => new Vector2I(-1, -1),
                6 => Vector2I.Up,
                7 => new Vector2I(1, -1),
                _ => Vector2I.Down
            };
        }
    }
}