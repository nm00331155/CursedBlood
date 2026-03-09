using CursedBlood.Player;
using Godot;

namespace CursedBlood.Camera
{
    public partial class GameCamera : Camera2D
    {
        private readonly RandomNumberGenerator _rng = new();
        private float _shakeTimer;
        private float _shakeIntensity;

        public PlayerController Target { get; set; }

        public override void _Ready()
        {
            SetProcess(true);
            Enabled = true;
            Position = new Vector2(540f, 960f);
            MakeCurrent();
        }

        public override void _Process(double delta)
        {
            var targetPosition = ResolveTargetPosition();

            if (_shakeTimer > 0f)
            {
                _shakeTimer = Mathf.Max(0f, _shakeTimer - (float)delta);
                targetPosition += new Vector2(
                    _rng.RandfRange(-_shakeIntensity, _shakeIntensity),
                    _rng.RandfRange(-_shakeIntensity, _shakeIntensity));
            }

            Position = Position.Lerp(targetPosition, Mathf.Clamp((float)delta * 10f, 0f, 1f));
        }

        public void SnapToTarget()
        {
            Position = ResolveTargetPosition();
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = Mathf.Max(0f, intensity);
            _shakeTimer = Mathf.Max(0f, duration);
        }

        private Vector2 ResolveTargetPosition()
        {
            if (Target == null)
            {
                return new Vector2(540f, 960f);
            }

            var targetPosition = Target.GetCurrentWorldPosition() + new Vector2(0f, 60f);
            targetPosition.X = 540f;
            targetPosition.Y = Mathf.Max(960f, targetPosition.Y);
            return targetPosition;
        }
    }
}