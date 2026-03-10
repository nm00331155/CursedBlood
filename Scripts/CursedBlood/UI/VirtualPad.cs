using Godot;

namespace CursedBlood.UI
{
    public sealed class VirtualPadSettings
    {
        public float BaseOpacity { get; set; } = 0.34f;

        public float KnobOpacity { get; set; } = 0.72f;

        public float DeadZoneRadius { get; set; } = 28f;

        public float MaxRadius { get; set; } = 82f;

        public float BaseRadius { get; set; } = 94f;

        public float KnobRadius { get; set; } = 38f;

        public float ReleaseRadiusMultiplier { get; set; } = 0.82f;

        public float DirectionHysteresisDegrees { get; set; } = 12f;

        public float GuideOpacity { get; set; } = 0.16f;

        public static VirtualPadSettings CreateDefault()
        {
            return new VirtualPadSettings();
        }
    }

    public partial class VirtualPad : Control
    {
        private Vector2 _origin;
        private Vector2 _currentPosition;
        private bool _active;
        private int _currentOctant = -1;
        private Vector2I _snappedDirection = Vector2I.Zero;

        public VirtualPadSettings Settings { get; private set; } = VirtualPadSettings.CreateDefault();

        public bool IsActive => _active;

        public Vector2 Origin => _origin;

        public Vector2 CurrentPosition => _currentPosition;

        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;
            ZIndex = 100;
            Visible = false;
        }

        public void ApplySettings(VirtualPadSettings settings)
        {
            Settings = settings ?? VirtualPadSettings.CreateDefault();
            QueueRedraw();
        }

        public void Begin(Vector2 origin)
        {
            _active = true;
            _origin = origin;
            _currentPosition = origin;
            _currentOctant = -1;
            _snappedDirection = Vector2I.Zero;
            Visible = true;
            QueueRedraw();
        }

        public void UpdatePointer(Vector2 currentPosition)
        {
            if (!_active)
            {
                return;
            }

            _currentPosition = currentPosition;
            _snappedDirection = ResolveSnappedDirection();
            QueueRedraw();
        }

        public void End()
        {
            _active = false;
            _currentPosition = _origin;
            _currentOctant = -1;
            _snappedDirection = Vector2I.Zero;
            Visible = false;
            QueueRedraw();
        }

        public Vector2 GetDelta()
        {
            return _currentPosition - _origin;
        }

        public Vector2 GetClampedKnobOffset()
        {
            var delta = GetDelta();
            return delta == Vector2.Zero ? Vector2.Zero : delta.LimitLength(Settings.MaxRadius);
        }

        public Vector2I GetSnappedDirection()
        {
            return _snappedDirection;
        }

        public override void _Draw()
        {
            if (!_active)
            {
                return;
            }

            var baseAlpha = Mathf.Clamp(Settings.BaseOpacity, 0f, 1f);
            var knobAlpha = Mathf.Clamp(Settings.KnobOpacity, 0f, 1f);
            var baseColor = new Color(0.94f, 0.97f, 1f, baseAlpha);
            var rimColor = new Color(0.75f, 0.86f, 1f, Mathf.Clamp(baseAlpha + 0.12f, 0f, 1f));
            var knobColor = new Color(0.34f, 0.62f, 0.94f, knobAlpha);
            var knobOutline = new Color(0.95f, 0.98f, 1f, Mathf.Clamp(knobAlpha + 0.18f, 0f, 1f));
            var guideColor = new Color(0.92f, 0.97f, 1f, Settings.GuideOpacity);
            var knobCenter = _origin + GetClampedKnobOffset();

            DrawCircle(_origin, Settings.BaseRadius, baseColor);
            DrawArc(_origin, Settings.BaseRadius, 0f, Mathf.Tau, 48, rimColor, 3f);
            DrawCircle(_origin, Settings.DeadZoneRadius, new Color(0.96f, 0.98f, 1f, guideColor.A * 0.55f));

            for (var octant = 0; octant < 8; octant++)
            {
                var direction = GetDirectionForOctant(octant);
                var vector = new Vector2(direction.X, direction.Y).Normalized();
                var start = _origin + (vector * (Settings.DeadZoneRadius + 8f));
                var end = _origin + (vector * (Settings.BaseRadius - 10f));
                DrawLine(start, end, guideColor, octant == _currentOctant ? 2.8f : 1.4f);
            }

            DrawCircle(knobCenter, Settings.KnobRadius, knobColor);
            DrawArc(knobCenter, Settings.KnobRadius, 0f, Mathf.Tau, 32, knobOutline, 3f);
        }

        private Vector2I ResolveSnappedDirection()
        {
            var delta = GetDelta();
            var length = delta.Length();
            var releaseRadius = Settings.DeadZoneRadius * Mathf.Clamp(Settings.ReleaseRadiusMultiplier, 0.4f, 1f);
            if (length < releaseRadius)
            {
                _currentOctant = -1;
                return Vector2I.Zero;
            }

            if (length < Settings.DeadZoneRadius)
            {
                return _currentOctant >= 0 ? GetDirectionForOctant(_currentOctant) : Vector2I.Zero;
            }

            var angle = delta.Angle();
            var nearestOctant = Mathf.PosMod(Mathf.RoundToInt(angle / (Mathf.Pi / 4f)), 8);
            if (_currentOctant < 0)
            {
                _currentOctant = nearestOctant;
                return GetDirectionForOctant(_currentOctant);
            }

            var currentCenter = _currentOctant * (Mathf.Pi / 4f);
            var threshold = Mathf.DegToRad(22.5f + Settings.DirectionHysteresisDegrees);
            var angleToCurrent = Mathf.Abs(Mathf.AngleDifference(angle, currentCenter));
            if (angleToCurrent <= threshold)
            {
                return GetDirectionForOctant(_currentOctant);
            }

            _currentOctant = nearestOctant;
            return GetDirectionForOctant(_currentOctant);
        }

        private static Vector2I GetDirectionForOctant(int octant)
        {
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
                _ => Vector2I.Zero
            };
        }
    }
}