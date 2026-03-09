using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Player
{
    public partial class PlayerController : Node2D
    {
        [Export]
        public float SwipeThreshold { get; set; } = 40f;

        private const float PlayerRadius = 50f;

        private Vector2I _moveDirection = Vector2I.Down;
        private Vector2I _bufferedDirection = Vector2I.Zero;
        private bool _isMoving;
        private float _moveTimer;
        private float _currentMoveDuration;
        private Vector2I _moveTarget;
        private Vector2 _touchStartPosition;
        private bool _isTouching;
        private bool _isGuarding;
        private double _lastTapSeconds = -10d;
        private GameTheme _theme = ThemeSettings.CreateDefault().BuildTheme();

        public GridManager Grid { get; set; }

        public PlayerStats Stats { get; set; }

        public bool InputEnabled { get; set; } = true;

        public bool IsGuarding => _isGuarding;

        public Vector2I CurrentDirection => _moveDirection;

        public Action<CellData, bool> CellEntered { get; set; }

        public Action<Vector2I> SkillRequested { get; set; }

        public Func<Vector2I, Vector2I, bool> BossAttackRequested { get; set; }

        public override void _Process(double delta)
        {
            if (!InputEnabled || Stats == null || Grid == null || !Stats.IsAlive)
            {
                return;
            }

            HandleKeyboardInput();
            ProcessMovement((float)delta);
            QueueRedraw();
        }

        public override void _Input(InputEvent @event)
        {
            if (!InputEnabled || Stats == null || !Stats.IsAlive)
            {
                return;
            }

            if (@event is InputEventScreenTouch screenTouch)
            {
                if (screenTouch.Pressed)
                {
                    TryTriggerTapSkill();
                    _touchStartPosition = screenTouch.Position;
                    _isTouching = true;
                }
                else
                {
                    _isTouching = false;
                }
            }
            else if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    TryTriggerTapSkill();
                    _touchStartPosition = mouseButton.Position;
                    _isTouching = true;
                }
                else
                {
                    _isTouching = false;
                }
            }

            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Enter)
            {
                SkillRequested?.Invoke(_moveDirection);
            }

            if (@event is InputEventScreenDrag screenDrag)
            {
                DetectSwipe(screenDrag.Position);
            }
            else if (@event is InputEventMouseMotion mouseMotion && _isTouching)
            {
                DetectSwipe(mouseMotion.Position);
            }
        }

        public void ApplyTheme(GameTheme theme)
        {
            _theme = theme;
            QueueRedraw();
        }

        public Vector2 GetWorldPosition()
        {
            if (Stats == null || Grid == null)
            {
                return Vector2.Zero;
            }

            var basePosition = Grid.GridToWorld(Stats.GridPosition.X, Stats.GridPosition.Y);
            if (_isMoving && _currentMoveDuration > 0f)
            {
                var interpolation = Mathf.Min(_moveTimer / _currentMoveDuration, 1f);
                var targetPosition = Grid.GridToWorld(_moveTarget.X, _moveTarget.Y);
                return basePosition.Lerp(targetPosition, interpolation);
            }

            return basePosition;
        }

        public override void _Draw()
        {
            if (Stats == null || Grid == null)
            {
                return;
            }

            var position = GetWorldPosition();
            var playerColor = Stats.Phase switch
            {
                LifePhase.Youth => _theme.PlayerYouthColor,
                LifePhase.Prime => _theme.PlayerPrimeColor,
                LifePhase.Twilight => _theme.PlayerTwilightColor,
                _ => _theme.PlayerPrimeColor
            };

            DrawCircle(position, PlayerRadius, playerColor);
            DrawArc(position, PlayerRadius, 0f, Mathf.Tau, 48, _theme.BorderColor, 4f);

            var directionVector = new Vector2(_moveDirection.X, _moveDirection.Y);
            if (directionVector != Vector2.Zero)
            {
                directionVector = directionVector.Normalized();
                DrawLine(position, position + directionVector * (PlayerRadius + 16f), _theme.TextColor, 4f);
            }

            if (_isGuarding)
            {
                DrawArc(position, PlayerRadius + 12f, 0f, Mathf.Tau, 48, _theme.BorderColor, 3f);
            }
        }

        public void Reset()
        {
            _moveDirection = Vector2I.Down;
            _bufferedDirection = Vector2I.Zero;
            _isMoving = false;
            _moveTimer = 0f;
            _currentMoveDuration = 0f;
            _moveTarget = Vector2I.Zero;
            _isTouching = false;
            _isGuarding = false;
            _lastTapSeconds = -10d;
            QueueRedraw();
        }

        private void DetectSwipe(Vector2 currentPosition)
        {
            var difference = currentPosition - _touchStartPosition;
            if (difference.Length() < SwipeThreshold)
            {
                return;
            }

            Vector2I newDirection;
            if (Mathf.Abs(difference.X) > Mathf.Abs(difference.Y))
            {
                newDirection = difference.X > 0f ? Vector2I.Right : Vector2I.Left;
            }
            else
            {
                newDirection = difference.Y > 0f ? Vector2I.Down : Vector2I.Up;
            }

            SetDirection(newDirection);
            _touchStartPosition = currentPosition;
        }

        private void HandleKeyboardInput()
        {
            _isGuarding = Input.IsKeyPressed(Key.Space);

            if (Input.IsKeyPressed(Key.Up))
            {
                SetDirection(Vector2I.Up);
            }
            else if (Input.IsKeyPressed(Key.Down))
            {
                SetDirection(Vector2I.Down);
            }
            else if (Input.IsKeyPressed(Key.Left))
            {
                SetDirection(Vector2I.Left);
            }
            else if (Input.IsKeyPressed(Key.Right))
            {
                SetDirection(Vector2I.Right);
            }
        }

        private void SetDirection(Vector2I direction)
        {
            if (_isMoving)
            {
                _bufferedDirection = direction;
                return;
            }

            _moveDirection = direction;
        }

        private void ProcessMovement(float delta)
        {
            if (_isGuarding)
            {
                return;
            }

            if (!_isMoving)
            {
                if (_bufferedDirection != Vector2I.Zero)
                {
                    _moveDirection = _bufferedDirection;
                    _bufferedDirection = Vector2I.Zero;
                }

                TryStartMove();
            }

            if (!_isMoving)
            {
                return;
            }

            _moveTimer += delta;
            if (_moveTimer >= _currentMoveDuration)
            {
                CompleteMove();
            }
        }

        private void TryStartMove()
        {
            var target = Stats.GridPosition + _moveDirection;

            if (target.X < 0 || target.X >= GridManager.Columns || target.Y < 0)
            {
                return;
            }

            var cell = Grid.GetCell(target.X, target.Y);
            if (cell == null || !cell.IsDiggable)
            {
                return;
            }

            if (cell.HasBoss)
            {
                if (BossAttackRequested?.Invoke(Stats.GridPosition, _moveDirection) == true)
                {
                    return;
                }

                return;
            }

            _moveTarget = target;
            _moveTimer = 0f;
            _currentMoveDuration = Stats.GetMovementDuration(cell.Type, cell.Hardness);
            _isMoving = true;
        }

        private void CompleteMove()
        {
            _isMoving = false;
            var cell = Grid.GetCell(_moveTarget.X, _moveTarget.Y);
            var wasSolid = cell != null && cell.Type != CellType.Empty && cell.Type != CellType.Boss;

            Stats.GridPosition = _moveTarget;
            if (_moveTarget.Y > Stats.MaxDepth)
            {
                Stats.MaxDepth = _moveTarget.Y;
            }

            if (cell != null)
            {
                CellEntered?.Invoke(cell, wasSolid);
                if (cell.IsDiggable && cell.Type != CellType.Empty && cell.Type != CellType.Boss)
                {
                    cell.Dig();
                }
            }

            Grid.UpdateVisibleRange(Stats.GridPosition.Y);
        }

        private void TryTriggerTapSkill()
        {
            var currentTime = Time.GetTicksMsec() / 1000.0;
            if (currentTime - _lastTapSeconds <= 0.30d)
            {
                SkillRequested?.Invoke(_moveDirection);
                _lastTapSeconds = -10d;
                return;
            }

            _lastTapSeconds = currentTime;
        }
    }
}