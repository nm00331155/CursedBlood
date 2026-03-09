using System.Collections.Generic;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Player
{
    public partial class PlayerController : Node2D
    {
        private const float GuardHoldSeconds = 0.18f;

        private readonly List<Vector2I> _digBuffer = new(24);
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
        private float _pointerHoldTime;
        private float _moveTimer;
        private float _currentMoveDuration;
        private int _lastPlayerSize;
        private LifePhase _lastPhase;

        [Export]
        public float SwipeThreshold { get; set; } = 32f;

        public ChunkManager Chunks { get; set; }

        public PlayerStats Stats { get; set; }

        public bool InputEnabled { get; set; } = true;

        public bool IsGuarding => Input.IsKeyPressed(Key.Space) || _touchGuarding;

        public Vector2I CurrentDirection => _moveDirection;

        public override void _Ready()
        {
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
            var rect = new Rect2(-half, -half, sizePixels, sizePixels);

            var bodyColor = Stats.Phase switch
            {
                LifePhase.Youth => new Color(0.26f, 0.82f, 0.45f),
                LifePhase.Prime => new Color(0.25f, 0.58f, 0.92f),
                LifePhase.Twilight => new Color(0.93f, 0.55f, 0.20f),
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
            _pointerActive = false;
            _pointerMoved = false;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            _isMoving = false;
            _lastPlayerSize = Stats?.PlayerSize ?? 3;
            _lastPhase = Stats?.Phase ?? LifePhase.Youth;
            SyncToStatsPosition();
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
        }

        public Vector2 GetCurrentWorldPosition()
        {
            return GlobalPosition;
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
                QueueRedraw();
            }
        }

        private bool TryStartMove()
        {
            if (_moveDirection == Vector2I.Zero)
            {
                return false;
            }

            DigHelper.FillDigArea(_digBuffer, Stats.GridPosition, _moveDirection, Stats.DigWidth, Stats.DigShape, Stats.PlayerSize);
            if (_digBuffer.Count == 0)
            {
                return false;
            }

            var target = Stats.GridPosition + _moveDirection;
            var maxHardness = 1f;

            for (var index = 0; index < _digBuffer.Count; index++)
            {
                var cell = _digBuffer[index];
                var type = (CellType)Chunks.GetCell(cell.X, cell.Y);
                if (!CellTypeUtil.IsDiggable(type))
                {
                    return false;
                }

                maxHardness = Mathf.Max(maxHardness, CellTypeUtil.GetHardness(type));
            }

            if (!CanOccupyAfterDig(target, Stats.PlayerSize, _digBuffer))
            {
                return false;
            }

            var dugCount = DigHelper.ExecuteDig(Chunks, _digBuffer);
            if (dugCount > 0)
            {
                Stats.RegisterDig(dugCount);
            }

            _moveTargetGrid = target;
            _moveStartPosition = Chunks.GridToWorldCenter(Stats.GridPosition.X, Stats.GridPosition.Y);
            _moveEndPosition = Chunks.GridToWorldCenter(target.X, target.Y);
            _moveTimer = 0f;
            _currentMoveDuration = Stats.EffectiveMoveInterval * maxHardness;
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
        }

        private bool CanOccupyAfterDig(Vector2I center, int size, List<Vector2I> digArea)
        {
            var half = size / 2;
            for (var row = center.Y - half; row <= center.Y + half; row++)
            {
                for (var col = center.X - half; col <= center.X + half; col++)
                {
                    if (ContainsCell(digArea, col, row))
                    {
                        continue;
                    }

                    if ((CellType)Chunks.GetCell(col, row) == CellType.Bedrock)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool ContainsCell(List<Vector2I> cells, int col, int row)
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

        private void HandlePointerPressed(bool pressed, Vector2 position)
        {
            _pointerActive = pressed;
            _pointerMoved = false;
            _touchGuarding = false;
            _pointerHoldTime = 0f;
            _pointerStart = position;
        }

        private void DetectSwipe(Vector2 currentPosition)
        {
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