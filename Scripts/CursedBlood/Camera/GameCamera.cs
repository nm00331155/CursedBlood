using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Camera
{
    public partial class GameCamera : Camera2D
    {
        private readonly RandomNumberGenerator _rng = new();
        private float _shakeTimer;
        private float _shakeIntensity;
        private int _debugZoomIndex;
        private GameplayLayoutMetrics _layoutMetrics;
        private bool _hasLayout;
        private bool _layoutDebugLogging;
        private Vector2 _lastLoggedScreenSize = new(-1f, -1f);
        private float _lastLoggedEffectiveZoom = -1f;

        [Export]
        public float GameplayZoomScale { get; set; } = 0.28f;

        [Export]
        public float TightZoomScale { get; set; } = 0.22f;

        [Export]
        public float DefaultZoomScale { get; set; } = 0.38f;

        [Export]
        public float FollowLerpSpeed { get; set; } = 14.0f;

        [Export]
        public float LookAheadPixels { get; set; } = 54f;

        [Export]
        public Vector2 PlayfieldOffset { get; set; } = new Vector2(0f, -64f);

        [Export]
        public float SurfaceClampPadding { get; set; } = 72f;

        public PlayerController Target { get; set; }

        public override void _Ready()
        {
            SetProcess(true);
            Enabled = true;
            Position = new Vector2(540f, 960f);
            ApplyZoom(GetEffectiveZoomScale());
            MakeCurrent();
        }

        public override void _Process(double delta)
        {
            var effectiveZoom = GetEffectiveZoomScale();
            ApplyZoom(effectiveZoom);
            LogLayoutProjection(force: false, effectiveZoom);
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
            _debugZoomIndex = (_debugZoomIndex + 1) % 3;
            var effectiveZoom = GetEffectiveZoomScale();
            ApplyZoom(effectiveZoom);
            LogLayoutProjection(force: true, effectiveZoom);
            return $"base {GetActiveZoomScale():0.00} / effective {effectiveZoom:0.00}";
        }

        public void ApplyViewportLayout(GameplayLayoutMetrics layoutMetrics, bool enableDebugLogging = false)
        {
            _layoutMetrics = layoutMetrics;
            _hasLayout = layoutMetrics.ScreenSize != Vector2.Zero;
            _layoutDebugLogging = enableDebugLogging;

            var effectiveZoom = GetEffectiveZoomScale();
            ApplyZoom(effectiveZoom);
            LogLayoutProjection(force: true, effectiveZoom);
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
                return _hasLayout
                    ? _layoutMetrics.VisibleRect.Position + (_layoutMetrics.ScreenSize * 0.5f)
                    : new Vector2(540f, 960f);
            }

            var direction = new Vector2(Target.CurrentDirection.X, Target.CurrentDirection.Y);
            var lookAhead = direction == Vector2.Zero ? Vector2.Zero : direction.Normalized() * LookAheadPixels;
            var effectiveZoom = GetEffectiveZoomScale();
            var layoutOffset = _hasLayout
                ? GameplayLayoutCalculator.ResolveCameraWorldOffset(_layoutMetrics, effectiveZoom)
                : Vector2.Zero;
            var targetPosition = Target.GetCurrentWorldPosition() + PlayfieldOffset + layoutOffset + lookAhead;
            var halfView = GetViewportRect().Size * effectiveZoom * 0.5f;
            var surfaceWorldY = ChunkManager.WorldOriginY + (PlayerStats.SurfaceRow * ChunkManager.CellSize);
            var minY = surfaceWorldY + halfView.Y - SurfaceClampPadding;
            targetPosition.Y = Mathf.Max(minY, targetPosition.Y);
            return targetPosition;
        }

        private float GetEffectiveZoomScale()
        {
            return _hasLayout
                ? GameplayLayoutCalculator.ResolveProjection(_layoutMetrics, GetActiveZoomScale()).CameraZoomScale
                : GetActiveZoomScale();
        }

        private float GetActiveZoomScale()
        {
            return Mathf.Clamp(_debugZoomIndex, 0, 2) switch
            {
                0 => GameplayZoomScale,
                1 => TightZoomScale,
                _ => DefaultZoomScale
            };
        }

        private void ApplyZoom(float zoomScale)
        {
            var clampedZoom = Mathf.Clamp(zoomScale, 0.18f, 1.0f);
            var zoomVector = new Vector2(clampedZoom, clampedZoom);
            if (Zoom == zoomVector)
            {
                return;
            }

            Zoom = zoomVector;
        }

        private void LogLayoutProjection(bool force, float effectiveZoom)
        {
            if (!_layoutDebugLogging || !_hasLayout)
            {
                return;
            }

            if (!force && _lastLoggedScreenSize.IsEqualApprox(_layoutMetrics.ScreenSize) && Mathf.IsEqualApprox(_lastLoggedEffectiveZoom, effectiveZoom))
            {
                return;
            }

            var projection = GameplayLayoutCalculator.ResolveProjection(_layoutMetrics, GetActiveZoomScale());
            _lastLoggedScreenSize = _layoutMetrics.ScreenSize;
            _lastLoggedEffectiveZoom = effectiveZoom;
            GD.Print($"[Layout] world={projection.LogicalWorldSize.X:0.0}x{projection.LogicalWorldSize.Y:0.0} scale={projection.RenderScale:0.000} render={projection.RenderSize.X:0.0}x{projection.RenderSize.Y:0.0} zoom={projection.CameraZoomScale:0.000}");
        }
    }
}