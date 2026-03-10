using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Camera
{
    public partial class GameCamera : Camera2D
    {
        private readonly float[] _debugZoomPresets = new float[3];
        private readonly RandomNumberGenerator _rng = new();
        private float _shakeTimer;
        private float _shakeIntensity;
        private int _debugZoomIndex;

        [Export]
        public float GameplayZoomScale { get; set; } = 0.78f;

        [Export]
        public float TightZoomScale { get; set; } = 0.68f;

        [Export]
        public float DefaultZoomScale { get; set; } = 1.0f;

        [Export]
        public float FollowLerpSpeed { get; set; } = 8.5f;

        [Export]
        public float LookAheadPixels { get; set; } = 72f;

        [Export]
        public Vector2 PlayfieldOffset { get; set; } = new Vector2(0f, -112f);

        public PlayerController Target { get; set; }

        public override void _Ready()
        {
            SetProcess(true);
            Enabled = true;
            Position = new Vector2(540f, 960f);
            _debugZoomPresets[0] = GameplayZoomScale;
            _debugZoomPresets[1] = TightZoomScale;
            _debugZoomPresets[2] = DefaultZoomScale;
            ApplyZoom(GetActiveZoomScale());
            MakeCurrent();
        }

        public override void _Process(double delta)
        {
            ApplyZoom(GetActiveZoomScale());
            var targetPosition = ResolveTargetPosition();

            if (_shakeTimer > 0f)
            {
                _shakeTimer = Mathf.Max(0f, _shakeTimer - (float)delta);
                targetPosition += new Vector2(
                    _rng.RandfRange(-_shakeIntensity, _shakeIntensity),
                    _rng.RandfRange(-_shakeIntensity, _shakeIntensity));
            }

            Position = Position.Lerp(targetPosition, Mathf.Clamp((float)delta * FollowLerpSpeed, 0f, 1f));
        }

        public void SnapToTarget()
        {
            Position = ResolveTargetPosition();
        }

        public string CycleDebugZoomPreset()
        {
            _debugZoomIndex = (_debugZoomIndex + 1) % _debugZoomPresets.Length;
            ApplyZoom(GetActiveZoomScale());
            return $"{GetActiveZoomScale():0.00}";
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

            var direction = new Vector2(Target.CurrentDirection.X, Target.CurrentDirection.Y);
            var lookAhead = direction == Vector2.Zero ? Vector2.Zero : direction.Normalized() * LookAheadPixels;
            var targetPosition = Target.GetCurrentWorldPosition() + PlayfieldOffset + lookAhead;
            var halfView = GetViewportRect().Size * GetActiveZoomScale() * 0.5f;
            var minX = ChunkManager.FieldOffsetX + halfView.X;
            var maxX = ChunkManager.FieldOffsetX + ChunkManager.FieldWidth - halfView.X;
            targetPosition.X = minX <= maxX
                ? Mathf.Clamp(targetPosition.X, minX, maxX)
                : ChunkManager.FieldOffsetX + (ChunkManager.FieldWidth * 0.5f);
            targetPosition.Y = Mathf.Max(ChunkManager.FieldOffsetY + halfView.Y, targetPosition.Y);
            return targetPosition;
        }

        private float GetActiveZoomScale()
        {
            return _debugZoomPresets[Mathf.Clamp(_debugZoomIndex, 0, _debugZoomPresets.Length - 1)];
        }

        private void ApplyZoom(float zoomScale)
        {
            var clampedZoom = Mathf.Clamp(zoomScale, 0.45f, 1.2f);
            var zoomVector = new Vector2(clampedZoom, clampedZoom);
            if (Zoom == zoomVector)
            {
                return;
            }

            Zoom = zoomVector;
        }
    }
}