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
        private const float BlockFeedbackCooldownSeconds = 0.12f;
        private const int MousePointerId = -1;
        private const int NoPointerId = int.MinValue;

        private readonly List<Vector2I> _digBuffer = new(32);
        private readonly List<Vector2I> _occupancyBuffer = new(25);
        private readonly MoveDebugInfo _moveDebugInfo = new();

        private Vector2I _moveDirection = Vector2I.Down;
        private Vector2I _bufferedDirection = Vector2I.Zero;
        private Vector2I _moveTargetGrid;
        private Vector2I _lastBlockedCell = new(int.MinValue, int.MinValue);
        private MoveBlockReason _lastBlockedReason = MoveBlockReason.None;
        private Vector2 _moveStartPosition;
        private Vector2 _moveEndPosition;
        private Vector2 _pointerStart;
        private bool _pointerActive;
        private bool _pointerMoved;
        private bool _touchGuarding;
        private bool _isMoving;
        private float _pointerHoldTime;
        private float _moveTimer;
        private float _currentMoveDuration;
        private float _blockedFeedbackCooldown;
        private int _activePointerId = NoPointerId;
        private int _lastPlayerSize;
        private DivePhase _lastPhase;
        private string _movementStatusText = string.Empty;

        public ChunkManager Chunks { get; set; }

        public PlayerStats Stats { get; set; }

        public VirtualPad VirtualPad { get; set; }

        public bool InputEnabled { get; set; } = true;

        public bool IsGuarding => Input.IsKeyPressed(Key.Space) || _touchGuarding;

        public Vector2I CurrentDirection => _moveDirection;

        public MoveDebugInfo MoveDebugInfo => _moveDebugInfo;

        public string MovementStatusText => _movementStatusText;

        public event Action<long> SalvageCollected;

        public override void _Ready()
        {
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            if (_blockedFeedbackCooldown > 0f)
            {
                _blockedFeedbackCooldown = Mathf.Max(0f, _blockedFeedbackCooldown - (float)delta);
            }

            if (Stats == null || Chunks == null)
            {
                ClearMoveProbe(Vector2I.Zero);
                return;
            }

            if (!InputEnabled || !Stats.IsAlive)
            {
                ClearMoveProbe(Stats.GridPosition);
                return;
            }

            HandleKeyboardInput();
            UpdateTouchGuard((float)delta);
            ProcessMovement((float)delta);

            if (_lastPlayerSize != Stats.PlayerSize || _lastPhase != Stats.Phase)
            {
                _lastPlayerSize = Stats.PlayerSize;
                _lastPhase = Stats.Phase;
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
                    HandlePointerPressed(screenTouch.Pressed, screenTouch.Position, screenTouch.Index);
                    break;
                case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                    HandlePointerPressed(mouseButton.Pressed, mouseButton.Position, MousePointerId);
                    break;
                case InputEventScreenDrag screenDrag when _pointerActive && screenDrag.Index == _activePointerId:
                    HandlePointerDragged(screenDrag.Position);
                    break;
                case InputEventMouseMotion mouseMotion when _pointerActive && _activePointerId == MousePointerId:
                    HandlePointerDragged(mouseMotion.Position);
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
            var rect = new Rect2(-half, -half, sizePixels, sizePixels);

            var bodyColor = Stats.Phase switch
            {
                DivePhase.Stable => new Color(0.26f, 0.82f, 0.45f),
                DivePhase.Worn => new Color(0.92f, 0.69f, 0.20f),
                DivePhase.Critical => new Color(0.92f, 0.34f, 0.26f),
                _ => new Color(0.25f, 0.58f, 0.92f)
            };

            DrawRect(rect, bodyColor);
            DrawRect(rect, new Color(0.96f, 0.96f, 0.98f), false, 2f);

            if (_moveDirection != Vector2I.Zero)
            {
                var direction = new Vector2(_moveDirection.X, _moveDirection.Y).Normalized();
                DrawLine(Vector2.Zero, direction * (half + 12f), new Color(0.98f, 0.98f, 0.98f), 4f);
            }

            if (IsGuarding)
            {
                DrawArc(Vector2.Zero, half + 10f, 0f, Mathf.Tau, 48, new Color(0.75f, 0.92f, 1f), 4f);
            }
        }

        public void Reset()
        {
            _moveDirection = Vector2I.Down;
            _bufferedDirection = Vector2I.Zero;
            _moveTargetGrid = Vector2I.Zero;
            _moveTimer = 0f;
            _currentMoveDuration = 0f;
            _isMoving = false;
            _blockedFeedbackCooldown = 0f;
            _lastBlockedCell = new Vector2I(int.MinValue, int.MinValue);
            _lastBlockedReason = MoveBlockReason.None;
            _lastPlayerSize = Stats?.PlayerSize ?? 5;
            _lastPhase = Stats?.Phase ?? DivePhase.Stable;
            CancelTouchInput();
            ClearMoveProbe(Stats?.GridPosition ?? Vector2I.Zero);
            SyncToStatsPosition();
            QueueRedraw();
        }

        public void CancelTouchInput()
        {
            _pointerActive = false;
            _pointerMoved = false;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            _activePointerId = NoPointerId;
            VirtualPad?.End();
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
            UpdateMoveProbe();
        }

        public Vector2 GetCurrentWorldPosition()
        {
            return GlobalPosition;
        }

        public string GetDebugSummary()
        {
            if (!_moveDebugInfo.HasTarget)
            {
                return "Debug[F3] Idle";
            }

            var targetLabel = CellTypeUtil.GetName(_moveDebugInfo.TargetCellType);
            var slowLabel = _moveDebugInfo.MaxHardness > 1f
                ? $"{CellTypeUtil.GetName(_moveDebugInfo.SlowestCellType)} x{_moveDebugInfo.MaxHardness:0.0}"
                : "x1.0";

            if (_moveDebugInfo.CanMove)
            {
                return $"Debug[F3] Dir {_moveDebugInfo.Direction} Target {_moveDebugInfo.Target} Cell {targetLabel} Slow {slowLabel} Open";
            }

            var blockType = _moveDebugInfo.HasBlockedCell ? CellTypeUtil.GetName(_moveDebugInfo.BlockedCellType) : "None";
            var blockCell = _moveDebugInfo.HasBlockedCell ? _moveDebugInfo.BlockedCell.ToString() : "-";
            return $"Debug[F3] Dir {_moveDebugInfo.Direction} Target {_moveDebugInfo.Target} Cell {targetLabel} Slow {slowLabel} {GetBlockReasonLabel(_moveDebugInfo.BlockReason)} {blockType} {blockCell}";
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
            UpdateMoveProbe();

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
                UpdateMoveProbe();
            }

            TryStartMove();
        }

        private void UpdateMoveProbe()
        {
            if (Stats == null || Chunks == null)
            {
                ClearMoveProbe(Vector2I.Zero);
                return;
            }

            var probeDirection = _isMoving && _bufferedDirection != Vector2I.Zero ? _bufferedDirection : _moveDirection;
            EvaluateMove(probeDirection);
        }

        private void EvaluateMove(Vector2I direction)
        {
            _moveDebugInfo.Reset(Stats.GridPosition, direction);
            _movementStatusText = string.Empty;

            if (direction == Vector2I.Zero)
            {
                return;
            }

            var target = Stats.GridPosition + direction;
            DigHelper.FillDigArea(_digBuffer, Stats.GridPosition, direction, Stats.DigWidth, Stats.DigShape, Stats.PlayerSize);
            DigHelper.FillCenteredArea(_occupancyBuffer, target, Stats.PlayerSize);

            _moveDebugInfo.SetDigArea(_digBuffer);
            _moveDebugInfo.SetOccupancyArea(_occupancyBuffer);
            _moveDebugInfo.SetTargetCellType((CellType)Chunks.GetCell(target.X, target.Y));

            if (_digBuffer.Count == 0)
            {
                return;
            }

            for (var index = 0; index < _digBuffer.Count; index++)
            {
                var cell = _digBuffer[index];
                var type = (CellType)Chunks.GetCell(cell.X, cell.Y);

                if (!CellTypeUtil.IsDiggable(type))
                {
                    _moveDebugInfo.Block(ResolveFrontBlockReason(cell), cell, type);
                    _movementStatusText = BuildMovementStatus(_moveDebugInfo);
                    return;
                }

                _moveDebugInfo.ConsiderHardness(type);
            }

            for (var index = 0; index < _occupancyBuffer.Count; index++)
            {
                var cell = _occupancyBuffer[index];
                if (ContainsCell(_digBuffer, cell.X, cell.Y))
                {
                    continue;
                }

                var type = (CellType)Chunks.GetCell(cell.X, cell.Y);
                if (!CellTypeUtil.IsDiggable(type))
                {
                    _moveDebugInfo.Block(ResolveOccupancyBlockReason(cell), cell, type);
                    _movementStatusText = BuildMovementStatus(_moveDebugInfo);
                    return;
                }
            }

            _moveDebugInfo.AllowMove();
            _movementStatusText = BuildMovementStatus(_moveDebugInfo);
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
                QueueRedraw();
            }
        }

        private bool TryStartMove()
        {
            if (_moveDirection == Vector2I.Zero)
            {
                return false;
            }

            if (_moveDebugInfo.Direction != _moveDirection || _moveDebugInfo.Origin != Stats.GridPosition)
            {
                EvaluateMove(_moveDirection);
            }

            if (!_moveDebugInfo.CanMove)
            {
                EmitBlockedFeedback(_moveDebugInfo);
                return false;
            }

            var salvageDelta = 0L;
            for (var index = 0; index < _digBuffer.Count; index++)
            {
                var digCell = _digBuffer[index];
                var digType = (CellType)Chunks.GetCell(digCell.X, digCell.Y);
                salvageDelta += Stats.RegisterLoot(digType, digCell.Y);
            }

            var dugCount = DigHelper.ExecuteDig(Chunks, _digBuffer);
            if (dugCount > 0)
            {
                Stats.RegisterDig(dugCount);
            }

            if (salvageDelta > 0L)
            {
                SalvageCollected?.Invoke(salvageDelta);
            }

            _moveTargetGrid = Stats.GridPosition + _moveDirection;
            _moveStartPosition = Chunks.GridToWorldCenter(Stats.GridPosition.X, Stats.GridPosition.Y);
            _moveEndPosition = Chunks.GridToWorldCenter(_moveTargetGrid.X, _moveTargetGrid.Y);
            _moveTimer = 0f;
            _currentMoveDuration = Stats.EffectiveMoveInterval * _moveDebugInfo.MaxHardness;
            _isMoving = true;
            return true;
        }

        private void CompleteMove()
        {
            _isMoving = false;
            Stats.GridPosition = _moveTargetGrid;
            Stats.RegisterDepth(_moveTargetGrid.Y);
            Position = _moveEndPosition;
            Chunks.UpdateCamera(Stats.GridPosition.Y);
            UpdateMoveProbe();
        }

        private void EmitBlockedFeedback(MoveDebugInfo info)
        {
            if (!info.HasBlockedCell)
            {
                return;
            }

            if (_blockedFeedbackCooldown > 0f && info.BlockedCell == _lastBlockedCell && info.BlockReason == _lastBlockedReason)
            {
                return;
            }

            Chunks.FlashBlockedCell(info.BlockedCell, info.BlockReason);
            _blockedFeedbackCooldown = BlockFeedbackCooldownSeconds;
            _lastBlockedCell = info.BlockedCell;
            _lastBlockedReason = info.BlockReason;
        }

        private MoveBlockReason ResolveFrontBlockReason(Vector2I cell)
        {
            return Chunks.IsInBounds(cell.X, cell.Y) ? MoveBlockReason.Bedrock : MoveBlockReason.OutOfBounds;
        }

        private MoveBlockReason ResolveOccupancyBlockReason(Vector2I cell)
        {
            return Chunks.IsInBounds(cell.X, cell.Y) ? MoveBlockReason.Occupancy : MoveBlockReason.OutOfBounds;
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

        private void HandlePointerPressed(bool pressed, Vector2 position, int pointerId)
        {
            if (pressed)
            {
                if (_pointerActive)
                {
                    return;
                }

                _pointerActive = true;
                _pointerMoved = false;
                _touchGuarding = false;
                _pointerHoldTime = 0f;
                _pointerStart = position;
                _activePointerId = pointerId;
                VirtualPad?.Begin(position);
                return;
            }

            if (!_pointerActive || _activePointerId != pointerId)
            {
                return;
            }

            CancelTouchInput();
        }

        private void HandlePointerDragged(Vector2 currentPosition)
        {
            if (!_pointerActive)
            {
                return;
            }

            VirtualPad?.UpdatePointer(currentPosition);
            var delta = currentPosition - _pointerStart;
            var deadZone = VirtualPad?.Settings.DeadZoneRadius ?? 32f;
            if (delta.Length() < deadZone)
            {
                return;
            }

            _pointerMoved = true;
            _touchGuarding = false;
            _pointerHoldTime = 0f;

            var direction = VirtualPad?.GetSnappedDirection() ?? SnapToOctant(delta);
            RequestDirection(direction);
        }

        private void ClearMoveProbe(Vector2I origin)
        {
            _moveDebugInfo.Reset(origin, Vector2I.Zero);
            _movementStatusText = string.Empty;
        }

        private static string BuildMovementStatus(MoveDebugInfo info)
        {
            if (!info.HasTarget)
            {
                return string.Empty;
            }

            if (!info.CanMove)
            {
                return GetBlockReasonLabel(info.BlockReason);
            }

            if (info.MaxHardness >= 4f - 0.01f)
            {
                return "Digging: HardRock x4.0";
            }

            if (info.MaxHardness >= 2f - 0.01f)
            {
                return $"Digging: {CellTypeUtil.GetName(info.SlowestCellType)} x{info.MaxHardness:0.0}";
            }

            return string.Empty;
        }

        private static string GetBlockReasonLabel(MoveBlockReason reason)
        {
            return reason switch
            {
                MoveBlockReason.Bedrock => "Blocked: Bedrock",
                MoveBlockReason.Occupancy => "Blocked: Occupancy",
                MoveBlockReason.OutOfBounds => "Blocked: OutOfBounds",
                _ => "Blocked"
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